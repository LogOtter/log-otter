using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore;

public class SnapshotRepository<TBaseEvent, TSnapshot> where TBaseEvent : class, IEvent<TSnapshot> where TSnapshot : class, ISnapshot, new()
{
    private readonly EventStore _eventStore;
    private readonly IFeedIteratorFactory _feedIteratorFactory;
    private readonly EventStoreOptions _options;
    private readonly Container _snapshotContainer;

    public SnapshotRepository(
        CosmosContainer<TSnapshot> snapshotContainer,
        EventStoreDependency<TBaseEvent> eventStoreDependency,
        IFeedIteratorFactory feedIteratorFactory,
        IOptions<EventStoreOptions> options)
    {
        _feedIteratorFactory = feedIteratorFactory;
        _snapshotContainer = snapshotContainer.Container;
        _eventStore = eventStoreDependency.EventStore;
        _options = options.Value;
    }

    public async Task<int> CountSnapshotsAsync(
        string partitionKey,
        Func<IQueryable<TSnapshot>, IQueryable<TSnapshot>> applyQuery,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = CreateSnapshotQuery(partitionKey, includeDeleted);
        return await applyQuery(query).CountAsync(cancellationToken);
    }

    public async Task<int> CountSnapshotsAsync(string partitionKey, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        return await CountSnapshotsAsync(
            partitionKey,
            q => q,
            includeDeleted,
            cancellationToken);
    }

    public async Task<TSnapshot?> GetSnapshot(
        string id,
        string partitionKey,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var streamId = _options.EscapeIdIfRequired(id);
        var result = await GetSnapshotInternal(
            streamId,
            partitionKey,
            includeDeleted,
            cancellationToken);
        return result?.Snapshot;
    }

    public IAsyncEnumerable<TSnapshot> QuerySnapshots(string partitionKey, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        return QuerySnapshots(
            partitionKey,
            x => x,
            includeDeleted,
            cancellationToken);
    }

    public async IAsyncEnumerable<TResult> QuerySnapshots<TResult>(
        string partitionKey,
        Func<IQueryable<TSnapshot>, IQueryable<TResult>> applyQuery,
        bool includeDeleted = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

    public async Task ApplyEventsToSnapshot(
        string id,
        string partitionKey,
        ICollection<Event<TBaseEvent>> events,
        CancellationToken cancellationToken = default)
    {
        var streamId = _options.EscapeIdIfRequired(id);

        var result = await GetSnapshotInternal(
            streamId,
            partitionKey,
            true,
            cancellationToken);
        var snapshot = result.HasValue
            ? result.Value.Snapshot
            : new TSnapshot { Revision = 0, Id = streamId };
        var eTag = result?.ETag;

        var orderedEvents = events.OrderBy(e => e.EventNumber).ToList();
        var snapshotRevision = snapshot.Revision;
        var startingRevision = snapshotRevision + 1;
        var endRevision = snapshotRevision + orderedEvents.Count;

        if (orderedEvents.First().EventNumber != startingRevision || orderedEvents.Last().EventNumber != endRevision)
        {
            var allEvents = await _eventStore.ReadStreamForwards(
                streamId,
                startingRevision,
                int.MaxValue,
                cancellationToken);

            orderedEvents = allEvents.Select(Event<TBaseEvent>.FromStorageEvent).OrderBy(e => e.EventNumber).ToList();
        }

        foreach (var @event in orderedEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (snapshot.Revision >= @event.EventNumber)
            {
                continue;
            }

            @event.Body.Apply(snapshot);
            snapshot.Revision = @event.EventNumber;
        }

        await _snapshotContainer.UpsertItemAsync(
            snapshot,
            new PartitionKey(partitionKey),
            eTag == null
                ? null
                : new ItemRequestOptions { IfMatchEtag = eTag },
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task<(TSnapshot Snapshot, string ETag)?> GetSnapshotInternal(
        string streamId,
        string partitionKey,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _snapshotContainer.ReadItemAsync<TSnapshot>(
                streamId,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

            if (response.Resource.DeletedAt.HasValue && !includeDeleted)
            {
                return null;
            }

            return (response.Resource, response.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
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
