﻿using System.Collections.Concurrent;
using System.Net;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

internal class ContainerData
{
    private static readonly PartitionKey NonePartitionKey = new("###PartitionKeyNone###");
    private static readonly PartitionKey NullPartitionKey = new("###PartitionKeyNull###");
    private readonly ConcurrentDictionary<PartitionKey, ConcurrentDictionary<string, ContainerItem>> _data;
    private readonly int _defaultDocumentTimeToLive;
    private readonly UniqueKeyPolicy? _uniqueKeyPolicy;
    private readonly StringSerializationHelper _serializationHelper;

    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);

    private int _currentTimer;
    internal ConcurrentQueue<Action> DataChanges { get; } = new ConcurrentQueue<Action>();

    public ContainerData(UniqueKeyPolicy? uniqueKeyPolicy, int defaultDocumentTimeToLive, StringSerializationHelper serializationHelper)
    {
        GuardAgainstInvalidUniqueKeyPolicy(uniqueKeyPolicy);

        _uniqueKeyPolicy = uniqueKeyPolicy;
        _defaultDocumentTimeToLive = defaultDocumentTimeToLive;
        _serializationHelper = serializationHelper;

        _data = new ConcurrentDictionary<PartitionKey, ConcurrentDictionary<string, ContainerItem>>();
        _currentTimer = 0;
    }

    public event EventHandler<DataChangedEventArgs>? DataChanged;

    public IEnumerable<ContainerItem> GetAllItems()
    {
        return _data.Values.SelectMany(partition => partition.Values);
    }

    public IEnumerable<ContainerItem> GetItemsInPartition(PartitionKey? partitionKey)
    {
        // As of Microsoft.Azure.Cosmos v3 a null partition key causes a cross partition query
        return partitionKey.HasValue ? GetPartitionFromKey(partitionKey.Value).Values : GetAllItems();
    }

    public ContainerItem? GetItem(string id, PartitionKey partitionKey)
    {
        var partition = GetPartitionFromKey(partitionKey);

        return partition.TryGetValue(id, out var containerItem) ? containerItem : null;
    }

    public async Task<Response> AddItem(
        string json,
        PartitionKey partitionKey,
        DataChangeMode dataChangeMode,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        var id = JsonHelpers.GetIdFromJson(json);

        return await UpsertItem(
            json,
            partitionKey,
            dataChangeMode,
            requestOptions,
            cancellationToken,
            (partition) =>
            {
                if (partition.ContainsKey(id))
                {
                    throw new ObjectAlreadyExistsException();
                }
            }
        );
    }

    public async Task<Response> UpsertItem(
        string json,
        PartitionKey partitionKey,
        DataChangeMode dataChangeMode,
        ItemRequestOptions? requestOptions,
        CancellationToken cancellationToken,
        Action<ConcurrentDictionary<string, ContainerItem>>? partitionAssertion = null
    )
    {
        var partition = GetPartitionFromKey(partitionKey);
        var id = JsonHelpers.GetIdFromJson(json);
        var ttl = JsonHelpers.GetTtlFromJson(json, _defaultDocumentTimeToLive);

        GuardAgainstInvalidId(id);

        Response response;
        await _updateSemaphore.WaitAsync(cancellationToken);
        try
        {
            partitionAssertion?.Invoke(partition);
            var isUpdate = partition.TryGetValue(id, out var existingItem);

            if (existingItem != null)
            {
                if (existingItem.RequireETagOnNextUpdate)
                {
                    if (string.IsNullOrWhiteSpace(requestOptions?.IfMatchEtag))
                    {
                        throw new InvalidOperationException("An eTag must be provided as a concurrency exception is queued");
                    }
                }

                if (existingItem.HasScheduledETagMismatch)
                {
                    existingItem.ChangeETag();
                    throw new ETagMismatchException();
                }
            }

            if (IsUniqueKeyViolation(json, partition.Values.Where(i => i.Id != id)))
            {
                throw new UniqueConstraintViolationException();
            }

            if (isUpdate && requestOptions?.IfMatchEtag != null && requestOptions.IfMatchEtag != existingItem!.ETag)
            {
                throw new ETagMismatchException();
            }

            var newItem = new ContainerItem(id, json, partitionKey, GetExpiryTime(ttl, _currentTimer), _serializationHelper);

            partition[id] = newItem;

            response = new Response(newItem, isUpdate);
        }
        finally
        {
            _updateSemaphore.Release();
        }

        void ExecuteDataChange() =>
            DataChanged?.Invoke(
                this,
                new DataChangedEventArgs(response.IsUpdate ? Operation.Updated : Operation.Created, response.Item.Json, _serializationHelper)
            );
        if (dataChangeMode == DataChangeMode.Auto)
        {
            ExecuteDataChange();
        }
        else
        {
            DataChanges.Enqueue(ExecuteDataChange);
        }
        return response;
    }

    public async Task<Response> ReplaceItem(
        string id,
        string json,
        PartitionKey partitionKey,
        DataChangeMode dataChangeMode,
        ItemRequestOptions? requestOptions,
        CancellationToken cancellationToken
    )
    {
        var response = await UpsertItem(
            json,
            partitionKey,
            dataChangeMode,
            requestOptions,
            cancellationToken,
            (partition) =>
            {
                if (!partition.ContainsKey(id))
                {
                    throw new NotFoundException();
                }
            }
        );

        return response;
    }

    public ContainerItem RemoveItem(string id, PartitionKey partitionKey, DataChangeMode dataChangeMode, ItemRequestOptions? requestOptions)
    {
        var existingItem = GetItem(id, partitionKey);
        if (existingItem == null)
        {
            throw new NotFoundException();
        }

        if (requestOptions?.IfMatchEtag != null && requestOptions.IfMatchEtag != existingItem.ETag)
        {
            throw new ETagMismatchException();
        }

        var removedItem = RemoveItemInternal(id, partitionKey, dataChangeMode)!;

        return removedItem;
    }

    private ContainerItem? RemoveItemInternal(string id, PartitionKey partitionKey, DataChangeMode dataChangeMode)
    {
        var partition = GetPartitionFromKey(partitionKey);

        partition.Remove(id, out var removedItem);

        if (removedItem == null)
        {
            return removedItem;
        }

        void ExecuteDataChange() => DataChanged?.Invoke(this, new DataChangedEventArgs(Operation.Deleted, removedItem.Json, _serializationHelper));
        if (dataChangeMode == DataChangeMode.Auto)
        {
            ExecuteDataChange();
        }
        else
        {
            DataChanges.Enqueue(ExecuteDataChange);
        }

        return removedItem;
    }

    private void RemoveExpiredItems()
    {
        foreach (var partition in _data.Values)
        {
            foreach (var item in partition.Values)
            {
                if (!item.ExpiryTime.HasValue)
                {
                    continue;
                }

                if (_currentTimer < item.ExpiryTime.Value)
                {
                    continue;
                }

                RemoveItemInternal(item.Id, item.PartitionKey, DataChangeMode.Auto);
            }
        }
    }

    private bool IsUniqueKeyViolation(string json, IEnumerable<ContainerItem> otherItemsInPartition)
    {
        if (_uniqueKeyPolicy == null)
        {
            return false;
        }

        var items = otherItemsInPartition.ToList();

        foreach (var uniqueKey in _uniqueKeyPolicy.UniqueKeys)
        {
            var uniqueKeyValue = JsonHelpers.GetUniqueKeyValueFromJson(json, uniqueKey);
            if (items.Any(i => JsonHelpers.GetUniqueKeyValueFromJson(i.Json, uniqueKey).SetEquals(uniqueKeyValue)))
            {
                return true;
            }
        }

        return false;
    }

    private static void GuardAgainstInvalidId(string id)
    {
        if (id.Contains("/") || id.Contains(@"\") || id.Contains("#") || id.Contains("?"))
        {
            throw new InvalidOperationException(
                "CosmosDb does not escape the following characters: '/' '\\', '#', '?' in the URI when retrieving an item by ID. Please encode the ID to remove them"
            );
        }
    }

    private static void GuardAgainstInvalidUniqueKeyPolicy(UniqueKeyPolicy? uniqueKeyPolicy)
    {
        if (uniqueKeyPolicy == null)
        {
            return;
        }

        var uniqueKeyPaths = uniqueKeyPolicy.UniqueKeys.SelectMany(i => i.Paths).Select(p => p.TrimStart('/'));

        if (uniqueKeyPaths.Contains("id"))
        {
            throw new CosmosException(
                "The unique key path cannot contain system properties. 'id' is a system property.",
                HttpStatusCode.BadRequest,
                0,
                string.Empty,
                0
            );
        }
    }

    private ConcurrentDictionary<string, ContainerItem> GetPartitionFromKey(PartitionKey partitionKey)
    {
        var normalizedPartitionKey = partitionKey;

        if (partitionKey == PartitionKey.None)
        {
            normalizedPartitionKey = NonePartitionKey;
        }
        else if (partitionKey == PartitionKey.Null)
        {
            normalizedPartitionKey = NullPartitionKey;
        }

        return _data.GetOrAdd(normalizedPartitionKey, new ConcurrentDictionary<string, ContainerItem>());
    }

    private static int? GetExpiryTime(int ttl, int currentTimer)
    {
        if (ttl < 0)
        {
            return null;
        }

        return currentTimer + ttl;
    }

    public void Clear()
    {
        _data.Clear();

        _currentTimer = 0;
    }

    public void AdvanceTime(int seconds)
    {
        if (seconds < 0)
        {
            throw new ArgumentException("Seconds must be a positive value", nameof(seconds));
        }

        _currentTimer += seconds;

        RemoveExpiredItems();
    }

    public ContainerDataSnapshot CreateSnapshot()
    {
        using var sw = new StringWriter();
        using var json = new JsonTextWriter(sw);

        json.WriteStartObject();
        foreach (var (partitionKey, items) in _data)
        {
            json.WritePropertyName(partitionKey.ToString());
            json.WriteStartArray();
            foreach (var (_, containerItem) in items)
            {
                json.WriteStartObject();

                json.WritePropertyName("Id");
                json.WriteValue(containerItem.Id);

                json.WritePropertyName("PartitionKey");
                json.WriteValue(containerItem.PartitionKey.ToString());

                json.WritePropertyName("Json");
                json.WriteValue(containerItem.Json);

                json.WritePropertyName("ETag");
                json.WriteValue(containerItem.ETag);

                json.WritePropertyName("ExpiryType");
                json.WriteValue(containerItem.ExpiryTime);

                json.WritePropertyName("HasScheduledETagMismatch");
                json.WriteValue(containerItem.HasScheduledETagMismatch);

                json.WritePropertyName("RequireETagOnNextUpdate");
                json.WriteValue(containerItem.RequireETagOnNextUpdate);

                json.WriteEndObject();
            }

            json.WriteEndArray();
        }

        json.WriteEndObject();

        return new ContainerDataSnapshot(sw.ToString());
    }

    public void RestoreSnapshot(ContainerDataSnapshot snapshot)
    {
        var data = JObject.Parse(snapshot.Json);

        _data.Clear();
        foreach (var property in data.Properties())
        {
            var partitionKey = PartitionKeyHelpers.FromJsonString(property.Name);

            var partitionKeyDictionary = new ConcurrentDictionary<string, ContainerItem>();
            _data[partitionKey] = partitionKeyDictionary;

            var itemsArray = (JArray)property.Value;

            foreach (var item in itemsArray.Children<JObject>())
            {
                var containerItem = new ContainerItem(
                    item.Value<string>("Id")!,
                    item.Value<string>("Json")!,
                    PartitionKeyHelpers.FromJsonString(item.Value<string>("PartitionKey")!),
                    item.Value<int?>("ExpiryTime"),
                    item.Value<string>("ETag")!,
                    item.Value<bool>("RequireETagOnNextUpdate"),
                    item.Value<bool>("HasScheduledETagMismatch"),
                    _serializationHelper
                );

                partitionKeyDictionary[containerItem.Id] = containerItem;
            }
        }
    }
}
