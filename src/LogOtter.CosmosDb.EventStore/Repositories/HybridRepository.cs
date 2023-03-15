using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore;

public class HybridRepository<TBaseEvent, TSnapshot> where TBaseEvent : class, IEvent<TSnapshot> where TSnapshot : class, ISnapshot, new()
{
    private readonly EventRepository<TBaseEvent, TSnapshot> _eventRepository;
    private readonly EventStore _eventStore;
    private readonly IFeedIteratorFactory _feedIteratorFactory;
    private readonly EventStoreMetadata<TBaseEvent, TSnapshot> _metadata;
    private readonly EventStoreOptions _options;
    private readonly SnapshotRepository<TBaseEvent, TSnapshot> _snapshotRepository;

    public HybridRepository(
        EventStoreDependency<TBaseEvent> eventStoreDependency,
        EventRepository<TBaseEvent, TSnapshot> eventRepository,
        SnapshotRepository<TBaseEvent, TSnapshot> snapshotRepository,
        IFeedIteratorFactory feedIteratorFactory,
        IOptions<EventStoreOptions> options,
        EventStoreMetadata<TBaseEvent, TSnapshot> metadata)
    {
        _eventStore = eventStoreDependency.EventStore;
        _eventRepository = eventRepository;
        _snapshotRepository = snapshotRepository;
        _feedIteratorFactory = feedIteratorFactory;
        _metadata = metadata;
        _options = options.Value;
    }

    public async IAsyncEnumerable<TSnapshot?> QuerySnapshotsWithCatchupExpensivelyAsync(
        string partitionKey,
        Func<IQueryable<TSnapshot>, IQueryable<TSnapshot>> applyQuery,
        bool includeDeleted = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = _snapshotRepository.CreateSnapshotQuery(partitionKey, includeDeleted);

        query = applyQuery(query);

        using var feedIterator = _feedIteratorFactory.GetFeedIterator(query);

        while (feedIterator.HasMoreResults)
        {
            var batch = await feedIterator.ReadNextAsync(cancellationToken);
            foreach (var result in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return await ApplyNewEvents(
                    result,
                    _options.EscapeIdIfRequired(result.Id),
                    includeDeleted,
                    cancellationToken);
            }
        }
    }

    public async Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, params TBaseEvent[] events)
    {
        return await ApplyEvents(
            id,
            expectedRevision,
            CancellationToken.None,
            events);
    }

    public async Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, CancellationToken cancellationToken, params TBaseEvent[] events)
    {
        var snapshot = await _eventRepository.ApplyEvents(
            id,
            expectedRevision,
            cancellationToken,
            events);

        try
        {
            var partitionKey = _metadata.SnapshotPartitionKeyResolver(events.First());
            var eventRevision = expectedRevision.GetValueOrDefault(0);
            var eventsToUpdate = events.Select(e => new Event<TBaseEvent>(id, eventRevision++, e)).ToList();

            await _snapshotRepository.ApplyEventsToSnapshot(
                id,
                partitionKey,
                eventsToUpdate,
                cancellationToken);
        }
        catch
        {
            // We are updating the snapshot store immediately to ensure the read store is as up to date as possible.
            // If this fails, we'll fall back to the standard catch up subscription, which is why we are swallowing this exception
        }

        return snapshot;
    }

    private async Task<TSnapshot?> ApplyNewEvents(
        TSnapshot? snapshot,
        string streamId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var startPosition = snapshot?.Revision + 1 ?? 1;

        var eventStoreEvents = await _eventStore.ReadStreamForwards(
            streamId,
            startPosition,
            int.MaxValue,
            cancellationToken);

        var events = eventStoreEvents.Select(e => (TBaseEvent)e.EventBody).ToList();

        if (snapshot == null && !events.Any())
        {
            return null;
        }

        snapshot ??= new TSnapshot { Revision = 0, Id = streamId };

        foreach (var @event in events)
        {
            @event.Apply(snapshot);
            snapshot.Revision++;
        }

        if (snapshot.DeletedAt.HasValue && !includeDeleted)
        {
            return null;
        }

        return snapshot;
    }
}
