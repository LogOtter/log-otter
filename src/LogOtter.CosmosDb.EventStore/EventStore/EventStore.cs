using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore;

public class EventStore<TBaseEvent> : IEventStoreReader
    where TBaseEvent : class
{
    private const int MaxTransactionalBatchOperations = 100;

    private readonly Container _container;
    private readonly IFeedIteratorFactory _feedIteratorFactory;
    private readonly JsonSerializer _jsonSerializer;
    private readonly SimpleSerializationTypeMap _typeMap;

    internal EventStore(Container container, IFeedIteratorFactory feedIteratorFactory, SimpleSerializationTypeMap typeMap)
    {
        _container = container;
        _feedIteratorFactory = feedIteratorFactory;
        _typeMap = typeMap;
        _jsonSerializer = JsonSerializer.CreateDefault();
    }

    async Task<IReadOnlyCollection<IStorageEvent>> IEventStoreReader.ReadStreamForwards(string streamId, CancellationToken cancellationToken) =>
        (await ReadStreamForwards(streamId, cancellationToken)).Cast<IStorageEvent>().ToList();

    async Task<IReadOnlyCollection<IStorageEvent>> IEventStoreReader.ReadStreamForwards(
        string streamId,
        int startPosition,
        int numberOfEventsToRead,
        CancellationToken cancellationToken
    ) => (await ReadStreamForwards(streamId, startPosition, numberOfEventsToRead, cancellationToken)).Cast<IStorageEvent>().ToList();

    async Task<IStorageEvent> IEventStoreReader.ReadEventFromStream(string streamId, Guid eventId, CancellationToken cancellationToken) =>
        (await ReadEventFromStream(streamId, eventId, cancellationToken));

    public async Task AppendToStream(string streamId, int expectedVersion, params EventData<TBaseEvent>[] events)
    {
        await AppendToStream(streamId, expectedVersion, default, events);
    }

    public async Task AppendToStream(string streamId, int expectedVersion, CancellationToken cancellationToken, params EventData<TBaseEvent>[] events)
    {
        var storageEvents = new List<StorageEvent<TBaseEvent>>();
        var eventVersion = expectedVersion;

        foreach (var @event in events)
        {
            storageEvents.Add(new StorageEvent<TBaseEvent>(streamId, @event, ++eventVersion));
        }

        await AppendToStreamInternal(streamId, storageEvents, cancellationToken);
    }

    private async Task AppendToStreamInternal(
        string streamId,
        IEnumerable<StorageEvent<TBaseEvent>> events,
        CancellationToken cancellationToken = default
    )
    {
        var storageEvents = events.ToList();
        var firstEventNumber = storageEvents.First().EventNumber;

        try
        {
            var batchRequestOptions = new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = false };

            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId));
            foreach (var @event in storageEvents)
            {
                var cosmosDbStorageEvent = CosmosDbStorageEvent.FromStorageEvent(@event, _typeMap, _jsonSerializer);
                batch.CreateItem(cosmosDbStorageEvent, batchRequestOptions);
            }

            using var batchResponse =
                firstEventNumber == 1
                    ? await CreateEvents(batch, cancellationToken)
                    : await CreateEventsOnlyIfPreviousEventExists(batch, streamId, firstEventNumber - 1, cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict || ex.Headers["x-ms-substatus"] == "409" || ex.SubStatusCode == 409)
        {
            throw new ConcurrencyException($"Concurrency conflict when appending to stream {streamId}. Expected revision {firstEventNumber - 1}", ex);
        }
    }

    public async Task<IReadOnlyCollection<StorageEvent<TBaseEvent>>> ReadStreamForwards(
        string streamId,
        CancellationToken cancellationToken = default
    )
    {
        return await ReadStreamForwards(streamId, 1, int.MaxValue, cancellationToken);
    }

    public async Task<IReadOnlyCollection<StorageEvent<TBaseEvent>>> ReadStreamForwards(
        string streamId,
        int startPosition,
        int numberOfEventsToRead,
        CancellationToken cancellationToken = default
    )
    {
        if (startPosition < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(startPosition), "Start position should be greater than 0");
        }

        if (numberOfEventsToRead < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(startPosition), "Number of events to read should be greater than 0");
        }

        var endPosition = numberOfEventsToRead == int.MaxValue ? int.MaxValue : startPosition + numberOfEventsToRead - 1;

        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(streamId) };

        var query = _container
            .GetItemLinqQueryable<CosmosDbStorageEvent>(requestOptions: requestOptions)
            .Where(e => e.StreamId == streamId && e.EventNumber >= startPosition && e.EventNumber <= endPosition)
            .OrderBy(e => e.EventNumber)
            .Take(numberOfEventsToRead);

        var feedIterator = _feedIteratorFactory.GetFeedIterator(query);

        var events = new List<StorageEvent<TBaseEvent>>();

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);

            foreach (var resource in response.Resource)
            {
                events.Add(FromCosmosStorageEvent(resource));
            }
        }

        return events.AsReadOnly();
    }

    public async Task<int> ReadStreamEventCount(string streamId)
    {
        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(streamId) };
        return await _container
            .GetItemLinqQueryable<CosmosDbStorageEvent>(requestOptions: requestOptions)
            .Where(e => e.StreamId == streamId)
            .CountAsync();
    }

    public async Task<StorageEvent<TBaseEvent>> ReadEventFromStream(string streamId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(streamId) };
        var query = _container
            .GetItemLinqQueryable<CosmosDbStorageEvent>(requestOptions: requestOptions)
            .Where(e => e.StreamId == streamId && e.EventId == eventId);

        var feedIterator = _feedIteratorFactory.GetFeedIterator(query);

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);
            var storageEvent = response.SingleOrDefault();
            if (storageEvent != null)
            {
                return storageEvent.ToStorageEvent<TBaseEvent>(_typeMap, _jsonSerializer);
            }
        }

        throw new CosmosException($"Event {eventId} not found for stream '{streamId}'", HttpStatusCode.Conflict, 0, null, 0);
    }

    public StorageEvent<TBaseEvent> FromCosmosStorageEvent(CosmosDbStorageEvent cosmosDbStorageEvent)
    {
        return cosmosDbStorageEvent.ToStorageEvent<TBaseEvent>(_typeMap, _jsonSerializer);
    }

    public async Task CompactStream(string streamId, EventData<TBaseEvent> tombstone, CancellationToken cancellationToken = default)
    {
        var existingEvents = await ReadStreamForwards(streamId, cancellationToken);

        if (existingEvents.Count == 0)
        {
            return;
        }

        var tombstoneStorageEvent = new StorageEvent<TBaseEvent>(streamId, tombstone, 1);
        var tombstoneCosmosEvent = CosmosDbStorageEvent.FromStorageEvent(tombstoneStorageEvent, _typeMap, _jsonSerializer);
        var partitionKey = new PartitionKey(streamId);
        var batchRequestOptions = new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = false };

        // Highest event numbers first — these are deleted in the first batch alongside the tombstone replace.
        var eventNumbersToDelete = existingEvents.Where(e => e.EventNumber > 1).Select(e => e.EventNumber).OrderByDescending(n => n).ToList();

        // Phase A (atomic): replace event 1 with the tombstone, plus as many tail deletes as fit in a 100-op batch.
        // After this batch a crashed retry sees a stream whose first event is already the tombstone — projecting
        // through ICompactionEvent yields the correct (scrubbed) state without needing the deleted events.
        var firstBatch = _container.CreateTransactionalBatch(partitionKey);
        firstBatch.ReplaceItem(tombstoneCosmosEvent.Id, tombstoneCosmosEvent, batchRequestOptions);

        var firstBatchDeleteCount = Math.Min(eventNumbersToDelete.Count, MaxTransactionalBatchOperations - 1);
        for (var i = 0; i < firstBatchDeleteCount; i++)
        {
            firstBatch.DeleteItem($"{streamId}:{eventNumbersToDelete[i]}", batchRequestOptions);
        }

        using var firstBatchResponse = await firstBatch.ExecuteAsync(cancellationToken);
        if (!firstBatchResponse.IsSuccessStatusCode)
        {
            throw new CosmosException(
                firstBatchResponse.ErrorMessage,
                firstBatchResponse.StatusCode,
                0,
                firstBatchResponse.ActivityId,
                firstBatchResponse.RequestCharge
            );
        }

        // Phase B: delete any remaining tail events in 100-op batches.
        var remaining = eventNumbersToDelete.Skip(firstBatchDeleteCount).ToList();
        for (var offset = 0; offset < remaining.Count; offset += MaxTransactionalBatchOperations)
        {
            var batch = _container.CreateTransactionalBatch(partitionKey);
            var batchSize = Math.Min(MaxTransactionalBatchOperations, remaining.Count - offset);
            for (var i = 0; i < batchSize; i++)
            {
                batch.DeleteItem($"{streamId}:{remaining[offset + i]}", batchRequestOptions);
            }

            using var batchResponse = await batch.ExecuteAsync(cancellationToken);
            if (!batchResponse.IsSuccessStatusCode)
            {
                throw new CosmosException(batchResponse.ErrorMessage, batchResponse.StatusCode, 0, batchResponse.ActivityId, batchResponse.RequestCharge);
            }
        }
    }

    private static async Task<TransactionalBatchResponse> CreateEvents(TransactionalBatch batch, CancellationToken cancellationToken)
    {
        var batchResponse = await batch.ExecuteAsync(cancellationToken);

        if (!batchResponse.IsSuccessStatusCode)
        {
            throw new CosmosException(batchResponse.ErrorMessage, batchResponse.StatusCode, 0, batchResponse.ActivityId, batchResponse.RequestCharge);
        }

        return batchResponse;
    }

    private static async Task<TransactionalBatchResponse> CreateEventsOnlyIfPreviousEventExists(
        TransactionalBatch batch,
        string streamId,
        int previousEventNumber,
        CancellationToken cancellationToken
    )
    {
        var requestOptions = new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = true };

        batch.ReadItem($"{streamId}:{previousEventNumber}", requestOptions);

        var batchResponse = await batch.ExecuteAsync(cancellationToken);

        if (!batchResponse.IsSuccessStatusCode)
        {
            throw batchResponse.StatusCode switch
            {
                HttpStatusCode.NotFound => new CosmosException(
                    $"Previous Event {previousEventNumber} not found for stream '{streamId}'",
                    HttpStatusCode.Conflict,
                    0,
                    batchResponse.ActivityId,
                    batchResponse.RequestCharge
                ),
                _ => new CosmosException(
                    batchResponse.ErrorMessage,
                    batchResponse.StatusCode,
                    0,
                    batchResponse.ActivityId,
                    batchResponse.RequestCharge
                ),
            };
        }

        return batchResponse;
    }
}
