using System.Collections.Concurrent;
using System.Net;
using System.Text;
using LogOtter.CosmosDb.ContainerMock.ContainerMockData;
using LogOtter.CosmosDb.ContainerMock.TransactionalBatch;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using Newtonsoft.Json;

#pragma warning disable CS1998

namespace LogOtter.CosmosDb.ContainerMock;

public class ContainerMock : Container
{
    private readonly ContainerData _containerData;

    private readonly ConcurrentQueue<(CosmosException exception, Func<InvocationInformation, bool> condition)> _exceptionsToThrow;

    private readonly string _partitionKeyPath;
    private readonly JsonSerializerSettings? _jsonSerializerSettings;

    public override string Id { get; }
    public override Conflicts? Conflicts => null;
    public override Scripts? Scripts => null;
    public override Database? Database => null;

    public ContainerMock(
        string partitionKeyPath = "/partitionKey",
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        string containerName = "TestContainer",
        int defaultDocumentTimeToLive = -1,
        JsonSerializerSettings? jsonSerializerSettings = null
    )
    {
        _containerData = new ContainerData(uniqueKeyPolicy, defaultDocumentTimeToLive, jsonSerializerSettings);
        _exceptionsToThrow = new ConcurrentQueue<(CosmosException, Func<InvocationInformation, bool> condition)>();

        _partitionKeyPath = partitionKeyPath;
        _jsonSerializerSettings = jsonSerializerSettings;

        Id = containerName;
    }

    public event EventHandler<DataChangedEventArgs> DataChanged
    {
        add => _containerData.DataChanged += value;
        remove => _containerData.DataChanged -= value;
    }

    public IEnumerable<TestContainerItem<T>> GetAllItems<T>()
    {
        var allItems = _containerData.GetAllItems();

        foreach (var containerItem in allItems)
        {
            var deserialized = containerItem.Deserialize<T>();
            yield return new TestContainerItem<T>(containerItem.PartitionKey.ToString(), containerItem.Id, deserialized);
        }
    }

    public override async Task<ResponseMessage> CreateItemStreamAsync(
        Stream streamPayload,
        PartitionKey partitionKey,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(CreateItemStreamAsync)));

        streamPayload.Position = 0;
        var streamReader = new StreamReader(streamPayload);
        var json = await streamReader.ReadToEndAsync();

        if (JsonHelpers.GetIdFromJson(json) == string.Empty)
        {
            return new ResponseMessage(HttpStatusCode.BadRequest);
        }

        try
        {
            var response = await _containerData.AddItem(json, partitionKey, requestOptions, cancellationToken);

            return ToCosmosResponseMessage(response, streamPayload);
        }
        catch (ContainerMockException ex)
        {
            return new ResponseMessage(ex.StatusCode);
        }
    }

    public override async Task<ItemResponse<T>> CreateItemAsync<T>(
        T item,
        PartitionKey? partitionKey = default,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(CreateItemAsync)));

        if (partitionKey != null && partitionKey == PartitionKey.None && _partitionKeyPath != null)
        {
            throw new CosmosException(
                "PartitionKey extracted from document doesn't match the one specified in the header. Learn more: https://aka.ms/CosmosDB/sql/errors/wrong-pk-value",
                HttpStatusCode.BadRequest,
                0,
                string.Empty,
                0
            );
        }

        var json = JsonConvert.SerializeObject(item);

        if (JsonHelpers.GetIdFromJson(json) == string.Empty)
        {
            throw new CosmosException(
                "Response status code does not indicate success: BadRequest (400);",
                HttpStatusCode.BadRequest,
                400,
                Guid.NewGuid().ToString(),
                0
            );
        }

        try
        {
            var response = await _containerData.AddItem(json, GetPartitionKey(json, partitionKey), requestOptions, cancellationToken);

            return ToCosmosItemResponse<T>(response);
        }
        catch (ContainerMockException ex)
        {
            throw ex.ToCosmosException();
        }
    }

    private PartitionKey GetPartitionKey(string json, PartitionKey? partitionKey)
    {
        return partitionKey ?? new PartitionKey(JsonHelpers.GetValueFromJson(json, _partitionKeyPath));
    }

    public override Task<ResponseMessage> ReadItemStreamAsync(
        string id,
        PartitionKey partitionKey,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(ReadItemStreamAsync)));

        if (id == string.Empty)
        {
            return Task.FromResult(new ResponseMessage(HttpStatusCode.BadRequest));
        }

        var item = _containerData.GetItem(id, partitionKey);

        if (item == null)
        {
            return Task.FromResult(new ResponseMessage(HttpStatusCode.NotFound));
        }

        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(item.Json));
        responseStream.Position = 0;

        var response = new ResponseMessage(HttpStatusCode.OK) { Content = responseStream };
        response.Headers.Add("etag", item.ETag);
        return Task.FromResult(response);
    }

    public override Task<ItemResponse<T>> ReadItemAsync<T>(
        string id,
        PartitionKey partitionKey,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(ReadItemAsync)));

        if (id == string.Empty)
        {
            throw new CosmosException(
                "Response status code does not indicate success: BadRequest (400);",
                HttpStatusCode.BadRequest,
                400,
                Guid.NewGuid().ToString(),
                0
            );
        }

        var item = _containerData.GetItem(id, partitionKey);

        if (item == null)
        {
            throw new NotFoundException().ToCosmosException();
        }

        var itemResponse = new MockItemResponse<T>(item.Deserialize<T>(), item.ETag);
        return Task.FromResult<ItemResponse<T>>(itemResponse);
    }

    public override async Task<ResponseMessage> UpsertItemStreamAsync(
        Stream streamPayload,
        PartitionKey partitionKey,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(UpsertItemStreamAsync)));

        streamPayload.Position = 0;
        var streamReader = new StreamReader(streamPayload);
        var json = await streamReader.ReadToEndAsync();

        try
        {
            var response = await _containerData.UpsertItem(json, partitionKey, requestOptions, cancellationToken);
            return ToCosmosResponseMessage(response, streamPayload);
        }
        catch (ContainerMockException ex)
        {
            return new ResponseMessage(ex.StatusCode);
        }
    }

    public override async Task<ItemResponse<T>> UpsertItemAsync<T>(
        T item,
        PartitionKey? partitionKey = default,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(UpsertItemAsync)));

        var json = JsonConvert.SerializeObject(item);

        try
        {
            var response = await _containerData.UpsertItem(json, GetPartitionKey(json, partitionKey), requestOptions, cancellationToken);
            return ToCosmosItemResponse<T>(response);
        }
        catch (ContainerMockException ex)
        {
            throw ex.ToCosmosException();
        }
    }

    public override async Task<ItemResponse<T>> ReplaceItemAsync<T>(
        T item,
        string id,
        PartitionKey? partitionKey = default,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(ReplaceItemAsync)));

        var json = JsonConvert.SerializeObject(item);

        try
        {
            var response = await _containerData.ReplaceItem(id, json, GetPartitionKey(json, partitionKey), requestOptions, cancellationToken);
            return ToCosmosItemResponse<T>(response);
        }
        catch (ContainerMockException ex)
        {
            throw ex.ToCosmosException();
        }
    }

    public Task<int> CountAsync<TModel>(string? partitionKey, Func<IQueryable<TModel>, IQueryable<TModel>> applyQuery)
    {
        if (applyQuery == null)
        {
            throw new ArgumentNullException(nameof(applyQuery));
        }

        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(CountAsync)));

        var partition = partitionKey == null ? (PartitionKey?)null : new PartitionKey(partitionKey);

        var items = _containerData.GetItemsInPartition(partition);

        var itemLinqQueryable = new CosmosQueryableMock<TModel>(items.OrderBy(i => i.Id).Select(i => i.Deserialize<TModel>()).AsQueryable());

        var queryable = applyQuery(itemLinqQueryable);
        return Task.FromResult(queryable.Count());
    }

    public override Task<ItemResponse<T>> DeleteItemAsync<T>(
        string id,
        PartitionKey partitionKey,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(DeleteItemAsync)));

        try
        {
            _containerData.RemoveItem(id, partitionKey, requestOptions);

            var itemResponse = new MockItemResponse<T>(HttpStatusCode.NoContent);
            return Task.FromResult<ItemResponse<T>>(itemResponse);
        }
        catch (ContainerMockException ex)
        {
            throw ex.ToCosmosException();
        }
    }

    public override IOrderedQueryable<T> GetItemLinqQueryable<T>(
        bool allowSynchronousQueryExecution = false,
        string? continuationToken = null,
        QueryRequestOptions? requestOptions = null,
        CosmosLinqSerializerOptions? linqSerializerOptions = null
    )
    {
        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(GetItemLinqQueryable)));

        var items = _containerData.GetItemsInPartition(requestOptions?.PartitionKey);

        var itemLinqQueryable = new CosmosQueryableMock<T>(items.OrderBy(i => i.Id).Select(i => i.Deserialize<T>()).AsQueryable());

        return itemLinqQueryable;
    }

    public async IAsyncEnumerable<TResult> QueryAsync<TModel, TResult>(string? partitionKey, Func<IQueryable<TModel>, IQueryable<TResult>> applyQuery)
    {
        if (applyQuery == null)
        {
            throw new ArgumentNullException(nameof(applyQuery));
        }

        ThrowNextExceptionIfPresent(new InvocationInformation(nameof(QueryAsync)));

        var partition = partitionKey == null ? (PartitionKey?)null : new PartitionKey(partitionKey);

        var items = _containerData.GetItemsInPartition(partition);

        var itemLinqQueryable = new CosmosQueryableMock<TModel>(items.OrderBy(i => i.Id).Select(i => i.Deserialize<TModel>()).AsQueryable());

        var results = applyQuery(itemLinqQueryable).ToList();

        foreach (var result in results)
        {
            yield return result;
        }
    }

    public override Microsoft.Azure.Cosmos.TransactionalBatch CreateTransactionalBatch(PartitionKey partitionKey)
    {
        return new TestTransactionalBatch(partitionKey, this, _jsonSerializerSettings);
    }

    public void Reset()
    {
        _containerData.Clear();
    }

    public void QueueExceptionToBeThrown(CosmosException exceptionToThrow, Func<InvocationInformation, bool>? condition = null)
    {
        condition ??= _ => true;
        _exceptionsToThrow.Enqueue((exceptionToThrow, condition));
    }

    public void TheNextWriteToDocumentRequiresEtagAndWillRaiseAConcurrencyException(PartitionKey partitionKey, string id)
    {
        var item = _containerData.GetItem(id, partitionKey);

        if (item == null)
        {
            throw new InvalidOperationException($"Could not find item '{id}' in partition '{partitionKey}'");
        }

        item.ScheduleMismatchETagOnNextUpdate();
    }

    public void AdvanceTime(int seconds)
    {
        _containerData.AdvanceTime(seconds);
    }

    public ContainerDataSnapshot CreateSnapshot()
    {
        return _containerData.CreateSnapshot();
    }

    public void RestoreSnapshot(ContainerDataSnapshot snapshot)
    {
        _containerData.RestoreSnapshot(snapshot);
    }

    private static ResponseMessage ToCosmosResponseMessage(Response response, Stream streamPayload)
    {
        var statusCode = response.IsUpdate ? HttpStatusCode.OK : HttpStatusCode.Created;

        var responseMessage = new ResponseMessage(statusCode) { Content = streamPayload };
        responseMessage.Headers.Add("etag", response.Item.ETag);

        return responseMessage;
    }

    private static ItemResponse<T> ToCosmosItemResponse<T>(Response response)
    {
        return new MockItemResponse<T>(
            response.Item.Deserialize<T>(),
            response.IsUpdate ? HttpStatusCode.OK : HttpStatusCode.Created,
            response.Item.ETag
        );
    }

    private void ThrowNextExceptionIfPresent(InvocationInformation invocationInformation)
    {
        if (!_exceptionsToThrow.TryPeek(out var peekedException))
        {
            return;
        }

        var condition = peekedException.condition(invocationInformation);
        if (!condition)
        {
            return;
        }

        if (_exceptionsToThrow.TryDequeue(out var exceptionToThrow) && peekedException == exceptionToThrow)
        {
            throw exceptionToThrow.exception;
        }
    }

    #region Not Implemented

    public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilderWithManualCheckpoint(
        string processorName,
        ChangeFeedStreamHandlerWithManualCheckpoint onChangesDelegate
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ContainerResponse> ReadContainerAsync(
        ContainerRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ResponseMessage> ReadContainerStreamAsync(
        ContainerRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ContainerResponse> ReplaceContainerAsync(
        ContainerProperties containerProperties,
        ContainerRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ResponseMessage> ReplaceContainerStreamAsync(
        ContainerProperties containerProperties,
        ContainerRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ContainerResponse> DeleteContainerAsync(
        ContainerRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ResponseMessage> DeleteContainerStreamAsync(
        ContainerRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<int?> ReadThroughputAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<ThroughputResponse> ReadThroughputAsync(RequestOptions requestOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<ThroughputResponse> ReplaceThroughputAsync(
        int throughput,
        RequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ThroughputResponse> ReplaceThroughputAsync(
        ThroughputProperties throughputProperties,
        RequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ResponseMessage> ReplaceItemStreamAsync(
        Stream streamPayload,
        string id,
        PartitionKey partitionKey,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ResponseMessage> ReadManyItemsStreamAsync(
        IReadOnlyList<(string id, PartitionKey partitionKey)> items,
        ReadManyRequestOptions? readManyRequestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<FeedResponse<T>> ReadManyItemsAsync<T>(
        IReadOnlyList<(string id, PartitionKey partitionKey)> items,
        ReadManyRequestOptions? readManyRequestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ItemResponse<T>> PatchItemAsync<T>(
        string id,
        PartitionKey partitionKey,
        IReadOnlyList<PatchOperation> patchOperations,
        PatchItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ResponseMessage> PatchItemStreamAsync(
        string id,
        PartitionKey partitionKey,
        IReadOnlyList<PatchOperation> patchOperations,
        PatchItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override Task<ResponseMessage> DeleteItemStreamAsync(
        string id,
        PartitionKey partitionKey,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public override FeedIterator GetItemQueryStreamIterator(
        QueryDefinition queryDefinition,
        string? continuationToken = null,
        QueryRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override FeedIterator<T> GetItemQueryIterator<T>(
        QueryDefinition queryDefinition,
        string? continuationToken = null,
        QueryRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override FeedIterator GetItemQueryStreamIterator(
        string? queryText = null,
        string? continuationToken = null,
        QueryRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override FeedIterator<T> GetItemQueryIterator<T>(
        string? queryText = null,
        string? continuationToken = null,
        QueryRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override FeedIterator GetItemQueryStreamIterator(
        FeedRange feedRange,
        QueryDefinition queryDefinition,
        string continuationToken,
        QueryRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override FeedIterator<T> GetItemQueryIterator<T>(
        FeedRange feedRange,
        QueryDefinition queryDefinition,
        string? continuationToken = null,
        QueryRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder<T>(string processorName, ChangesHandler<T> onChangesDelegate)
    {
        throw new NotImplementedException();
    }

    public override ChangeFeedProcessorBuilder GetChangeFeedEstimatorBuilder(
        string processorName,
        ChangesEstimationHandler estimationDelegate,
        TimeSpan? estimationPeriod = null
    )
    {
        throw new NotImplementedException();
    }

    public override ChangeFeedEstimator GetChangeFeedEstimator(string processorName, Container leaseContainer)
    {
        throw new NotImplementedException();
    }

    public override Task<IReadOnlyList<FeedRange>> GetFeedRangesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override FeedIterator GetChangeFeedStreamIterator(
        ChangeFeedStartFrom changeFeedStartFrom,
        ChangeFeedMode changeFeedMode,
        ChangeFeedRequestOptions? changeFeedRequestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override FeedIterator<T> GetChangeFeedIterator<T>(
        ChangeFeedStartFrom changeFeedStartFrom,
        ChangeFeedMode changeFeedMode,
        ChangeFeedRequestOptions? changeFeedRequestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder<T>(string processorName, ChangeFeedHandler<T> onChangesDelegate)
    {
        throw new NotImplementedException();
    }

    public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilderWithManualCheckpoint<T>(
        string processorName,
        ChangeFeedHandlerWithManualCheckpoint<T> onChangesDelegate
    )
    {
        throw new NotImplementedException();
    }

    public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder(string processorName, ChangeFeedStreamHandler onChangesDelegate)
    {
        throw new NotImplementedException();
    }

    #endregion
}
