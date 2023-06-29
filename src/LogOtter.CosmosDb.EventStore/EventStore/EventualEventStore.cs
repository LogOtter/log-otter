using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore;

public class EventualEventStore<TBaseEvent>
    where TBaseEvent : class
{
    private readonly Container _container;
    private readonly IFeedIteratorFactory _feedIteratorFactory;
    private readonly SimpleSerializationTypeMap _typeMap;
    private readonly JsonSerializer _jsonSerializer;

    internal EventualEventStore(Container container, IFeedIteratorFactory feedIteratorFactory, SimpleSerializationTypeMap typeMap)
    {
        _container = container;
        _feedIteratorFactory = feedIteratorFactory;
        _typeMap = typeMap;
        _jsonSerializer = JsonSerializer.CreateDefault();
    }

    public Task AddToStream(string streamId, CancellationToken cancellationToken, params EventualEventData<TBaseEvent>[] events)
    {
        var storageEvents = new List<EventualStorageEvent<TBaseEvent>>();

        foreach (var @event in events)
        {
            storageEvents.Add(new EventualStorageEvent<TBaseEvent>(streamId, @event));
        }

        return AddToStreamInternal(streamId, storageEvents, cancellationToken);
    }

    private async Task AddToStreamInternal(
        string streamId,
        IEnumerable<EventualStorageEvent<TBaseEvent>> events,
        CancellationToken cancellationToken = default
    )
    {
        var storageEvents = events.ToList();

        try
        {
            var batchRequestOptions = new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = false };

            var batch = _container.CreateTransactionalBatch(new PartitionKey(streamId));
            foreach (var @event in storageEvents)
            {
                var cosmosDbStorageEvent = CosmosDbEventualStorageEvent.FromStorageEvent(@event, _typeMap, _jsonSerializer);
                batch.CreateItem(cosmosDbStorageEvent, batchRequestOptions);
            }

            // ReSharper disable once UnusedVariable
            var batchResponse = await CreateEvents(batch, cancellationToken);

            //_loggingOptions.OnSuccess(ResponseInformation.FromWriteResponse(nameof(AppendToStream), batchResponse));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict || ex.Headers["x-ms-substatus"] == "409" || ex.SubStatusCode == 409)
        {
            throw new ConcurrencyException($"Concurrency conflict when appending to stream {streamId}.", ex);
        }
    }

    public async Task<IReadOnlyCollection<EventualStorageEvent<TBaseEvent>>> ReadStreamForwards(string streamId, CancellationToken cancellationToken)
    {
        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(streamId) };

        var query = _container
            .GetItemLinqQueryable<CosmosDbEventualStorageEvent>(requestOptions: requestOptions)
            .Where(e => e.StreamId == streamId)
            .OrderBy(e => e.Timestamp);

        var feedIterator = _feedIteratorFactory.GetFeedIterator(query);

        var events = new List<EventualStorageEvent<TBaseEvent>>();

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);
            //_loggingOptions.OnSuccess(ResponseInformation.FromReadResponse(nameof(ReadStreamForwards), response));

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
            .GetItemLinqQueryable<CosmosDbEventualStorageEvent>(requestOptions: requestOptions)
            .Where(e => e.StreamId == streamId)
            .CountAsync();
    }

    public async Task<EventualStorageEvent<TBaseEvent>> ReadEventFromStream(
        string streamId,
        Guid eventId,
        CancellationToken cancellationToken = default
    )
    {
        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(streamId) };
        var query = _container
            .GetItemLinqQueryable<CosmosDbEventualStorageEvent>(requestOptions: requestOptions)
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

    public EventualStorageEvent<TBaseEvent> FromCosmosStorageEvent(CosmosDbEventualStorageEvent cosmosDbStorageEvent)
    {
        return cosmosDbStorageEvent.ToStorageEvent<TBaseEvent>(_typeMap, _jsonSerializer);
    }

    private static async Task<TransactionalBatchResponse> CreateEvents(TransactionalBatch batch, CancellationToken cancellationToken)
    {
        using var batchResponse = await batch.ExecuteAsync(cancellationToken);

        if (!batchResponse.IsSuccessStatusCode)
        {
            throw new CosmosException(batchResponse.ErrorMessage, batchResponse.StatusCode, 0, batchResponse.ActivityId, batchResponse.RequestCharge);
        }

        return batchResponse;
    }
}
