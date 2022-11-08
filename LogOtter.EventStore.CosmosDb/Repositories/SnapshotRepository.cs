using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace LogOtter.EventStore.CosmosDb;

public class SnapshotRepository<TBaseEvent, TSnapshot>
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    private readonly Container _snapshotContainer;
    private readonly EventStore _eventStore;

    public SnapshotRepository(CosmosContainer<TSnapshot> snapshotContainer,
        EventStoreDependency<TBaseEvent> eventStoreDependency)
    {
        _snapshotContainer = snapshotContainer.Container;
        _eventStore = eventStoreDependency.EventStore;
    }

    public async Task<TSnapshot?> GetSnapshot(string id, string partitionKey, bool includeDeleted = false)
    {
        var streamId = CosmosHelpers.EscapeForCosmosId(id);
        var result = await GetSnapshotInternal(streamId, partitionKey, includeDeleted);
        return result?.Snapshot;
    }

    public IAsyncEnumerable<TSnapshot> QuerySnapshots(
        string partitionKey,
        bool includeDeleted = false
    )
    {
        return QuerySnapshots(partitionKey, x => x, includeDeleted);
    }

    public async IAsyncEnumerable<TResult> QuerySnapshots<TResult>(
        string partitionKey,
        Func<IQueryable<TSnapshot>, IQueryable<TResult>> applyQuery,
        bool includeDeleted = false
    )
    {
        var query = CreateSnapshotQuery(partitionKey, includeDeleted);

        var projectedQuery = applyQuery(query);

        using var feedIterator = projectedQuery.ToFeedIterator();

        while (feedIterator.HasMoreResults)
        {
            var batch = await feedIterator.ReadNextAsync();
            foreach (var result in batch)
            {
                yield return result;
            }
        }
    }

    public async Task ApplyEventsToSnapshot(string id, string partitionKey, ICollection<Event<TBaseEvent>> events,
        CancellationToken cancellationToken = default)
    {
        var streamId = CosmosHelpers.EscapeForCosmosId(id);

        var result = await GetSnapshotInternal(streamId, partitionKey, true);
        var snapshot = result.HasValue ? result.Value.Snapshot : new TSnapshot { Revision = 0, Id = streamId };
        var eTag = result?.ETag;

        var orderedEvents = events.OrderBy(e => e.EventNumber).ToList();
        var snapshotRevision = snapshot.Revision;
        var startingRevision = snapshotRevision + 1;
        var endRevision = snapshotRevision + orderedEvents.Count;

        if (orderedEvents.First().EventNumber != startingRevision || orderedEvents.Last().EventNumber != endRevision)
        {
            var allEvents = await _eventStore
                .ReadStreamForwards(streamId, startingRevision, int.MaxValue, cancellationToken);

            orderedEvents = allEvents
                .Select(e => Event<TBaseEvent>.FromStorageEvent(e))
                .OrderBy(e => e.EventNumber)
                .ToList();
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
            requestOptions: eTag == null ? null : new ItemRequestOptions { IfMatchEtag = eTag },
            cancellationToken: cancellationToken
        );

        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task<(TSnapshot Snapshot, string ETag)?> GetSnapshotInternal(
        string streamId,
        string partitionKey,
        bool includeDeleted
    )
    {
        try
        {
            var response = await _snapshotContainer.ReadItemAsync<TSnapshot>(streamId, new PartitionKey(partitionKey));

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

    private IQueryable<TSnapshot> CreateSnapshotQuery(string? partitionKey, bool includeDeleted)
    {
        var requestOptions = new QueryRequestOptions();

        if (partitionKey != null)
        {
            requestOptions.PartitionKey = new PartitionKey(partitionKey);
        }

        var query = (IQueryable<TSnapshot>)_snapshotContainer.GetItemLinqQueryable<TSnapshot>(
            false,
            requestOptions: requestOptions
        );

        if (!includeDeleted)
        {
            query = query.Where(x => x.DeletedAt == null);
        }

        return query;
    }
}