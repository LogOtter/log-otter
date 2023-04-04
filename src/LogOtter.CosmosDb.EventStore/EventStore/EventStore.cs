using System.Net;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.EventStore;

public class EventStore<TBaseEvent>
    where TBaseEvent : class
{
    private readonly Container _container;
    private readonly IFeedIteratorFactory _feedIteratorFactory;

    public EventStore(Container container, IFeedIteratorFactory feedIteratorFactory)
    {
        _container = container;
        _feedIteratorFactory = feedIteratorFactory;
    }

    public Task AppendToStream(string streamId, int expectedVersion, params EventData<TBaseEvent>[] events)
    {
        return AppendToStream(streamId, expectedVersion, default, events);
    }

    public Task AppendToStream(string streamId, int expectedVersion, CancellationToken cancellationToken, params EventData<TBaseEvent>[] events)
    {
        var storageEvents = new List<StorageEvent<TBaseEvent>>();
        var eventVersion = expectedVersion;
        var date = DateTimeOffset.Now;

        foreach (var @event in events)
        {
            storageEvents.Add(new StorageEvent<TBaseEvent>(streamId, @event, ++eventVersion, date));
        }

        return AppendToStreamInternal(streamId, storageEvents, cancellationToken);
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
                batch.CreateItem(@event, batchRequestOptions);
            }

            // ReSharper disable once UnusedVariable
            var batchResponse =
                firstEventNumber == 1
                    ? await CreateEvents(batch, cancellationToken)
                    : await CreateEventsOnlyIfPreviousEventExists(batch, streamId, firstEventNumber - 1, cancellationToken);

            //_loggingOptions.OnSuccess(ResponseInformation.FromWriteResponse(nameof(AppendToStream), batchResponse));
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
        var endPosition = numberOfEventsToRead == int.MaxValue ? int.MaxValue : startPosition + numberOfEventsToRead - 1;

        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(streamId) };

        var query = _container
            .GetItemLinqQueryable<StorageEvent<TBaseEvent>>(requestOptions: requestOptions)
            .Where(e => e.StreamId == streamId && e.EventNumber >= startPosition && e.EventNumber <= endPosition)
            .OrderBy(e => e.EventNumber)
            .Take(numberOfEventsToRead);

        var feedIterator = _feedIteratorFactory.GetFeedIterator(query);

        var events = new List<StorageEvent<TBaseEvent>>();

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);
            //_loggingOptions.OnSuccess(ResponseInformation.FromReadResponse(nameof(ReadStreamForwards), response));

            foreach (var resource in response.Resource)
            {
                events.Add(resource);
            }
        }

        return events.AsReadOnly();
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

    private static async Task<TransactionalBatchResponse> CreateEventsOnlyIfPreviousEventExists(
        TransactionalBatch batch,
        string streamId,
        int previousEventNumber,
        CancellationToken cancellationToken
    )
    {
        var requestOptions = new TransactionalBatchItemRequestOptions { EnableContentResponseOnWrite = true };

        batch.ReadItem($"{streamId}:{previousEventNumber}", requestOptions);

        using var batchResponse = await batch.ExecuteAsync(cancellationToken);

        if (!batchResponse.IsSuccessStatusCode)
        {
            throw batchResponse.StatusCode switch
            {
                HttpStatusCode.NotFound
                    => new CosmosException(
                        $"Previous Event {previousEventNumber} not found for stream '{streamId}'",
                        HttpStatusCode.Conflict,
                        0,
                        batchResponse.ActivityId,
                        batchResponse.RequestCharge
                    ),
                _
                    => new CosmosException(
                        batchResponse.ErrorMessage,
                        batchResponse.StatusCode,
                        0,
                        batchResponse.ActivityId,
                        batchResponse.RequestCharge
                    )
            };
        }

        return batchResponse;
    }
}
