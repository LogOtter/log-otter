using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore;

public class EventualSnapshotRepository<TBaseEvent, TSnapshot>
    where TBaseEvent : class, IEventualEvent<TSnapshot>
    where TSnapshot : class, IEventualSnapshot, new()
{
    private readonly EventualEventStore<TBaseEvent> _eventStore;
    private readonly IFeedIteratorFactory _feedIteratorFactory;
    private readonly EventStoreOptions _options;
    private readonly Container _snapshotContainer;

    public EventualSnapshotRepository(
        CosmosContainer<TSnapshot> snapshotContainer,
        EventualEventStore<TBaseEvent> eventStore,
        IFeedIteratorFactory feedIteratorFactory,
        IOptions<EventStoreOptions> options
    )
    {
        _feedIteratorFactory = feedIteratorFactory;
        _snapshotContainer = snapshotContainer.Container;
        _eventStore = eventStore;
        _options = options.Value;
    }

    public async Task<int> CountSnapshotsAsync(
        string partitionKey,
        Func<IQueryable<TSnapshot>, IQueryable<TSnapshot>> applyQuery,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default
    )
    {
        var query = CreateSnapshotQuery(partitionKey, includeDeleted);
        return await applyQuery(query).CountAsync(cancellationToken);
    }

    public async Task<int> CountSnapshotsAsync(string partitionKey, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        return await CountSnapshotsAsync(partitionKey, q => q, includeDeleted, cancellationToken);
    }

    public async Task<TSnapshot?> GetSnapshot(
        string id,
        string partitionKey,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default
    )
    {
        var streamId = _options.EscapeIdIfRequired(id);

        try
        {
            var response = await _snapshotContainer.ReadItemAsync<TSnapshot>(
                streamId,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken
            );

            if (response.Resource.DeletedAt.HasValue && !includeDeleted)
            {
                return null;
            }

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public IAsyncEnumerable<TSnapshot> QuerySnapshots(string partitionKey, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        return QuerySnapshots(partitionKey, x => x, includeDeleted, cancellationToken);
    }

    public async IAsyncEnumerable<TResult> QuerySnapshots<TResult>(
        string partitionKey,
        Func<IQueryable<TSnapshot>, IQueryable<TResult>> applyQuery,
        bool includeDeleted = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var query = CreateSnapshotQuery(partitionKey, includeDeleted);

        var projectedQuery = applyQuery(query);

        using var feedIterator = _feedIteratorFactory.GetFeedIterator(projectedQuery);

        while (feedIterator.HasMoreResults)
        {
            var batch = await feedIterator.ReadNextAsync(cancellationToken);
            foreach (var result in batch)
            {
                yield return result;
            }
        }
    }

    public async Task UpdateSnapshot(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        var streamId = _options.EscapeIdIfRequired(id);

        var events = await _eventStore.ReadStreamForwards(streamId, cancellationToken);

        var orderedEvents = events.OrderBy(e => e.Timestamp).ThenBy(e => e.EventId).ToList();

        var snapshot = new TSnapshot { Id = streamId };

        foreach (var @event in orderedEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            @event.EventBody.Apply(snapshot);
            snapshot.LastUpdated = @event.EventBody.Timestamp;
        }

        await _snapshotContainer.UpsertItemAsync(snapshot, new PartitionKey(partitionKey), cancellationToken: cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
    }

    internal IQueryable<TSnapshot> CreateSnapshotQuery(string? partitionKey, bool includeDeleted)
    {
        var requestOptions = new QueryRequestOptions();

        if (partitionKey != null)
        {
            requestOptions.PartitionKey = new PartitionKey(partitionKey);
        }

        var query = (IQueryable<TSnapshot>)_snapshotContainer.GetItemLinqQueryable<TSnapshot>(requestOptions: requestOptions);

        if (!includeDeleted)
        {
            query = query.Where(x => x.DeletedAt == null);
        }

        return query;
    }
}
