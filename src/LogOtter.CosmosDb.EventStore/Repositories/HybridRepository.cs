using System.Runtime.CompilerServices;
using LogOtter.CosmosDb.EventStore.Metadata;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore;

/// <summary>
/// Uses the snapshot store and event stream in tandem to produce immediately consistent results at the cost of performance
/// This repository is suitable for use when:
///   * You are working with a relatively small amount of data (but not a single entity)
///   * You are working with a single entity that has a large number of events in the stream
///   * You require immediately consistent results
///
/// If you are working with a single entity the <see cref="EventRepository{TBaseEvent, TSnapshot}"/> is possibly a better choice if the number of events in the stream will not be large
/// If you are working with lots of data, the <see cref="SnapshotRepository{TBaseEvent, TSnapshot}"/> is almost definitely a better choice
/// Requirements for large amounts of immediately consistent data should be stored in an immediate state store, not an event sourced system.
/// </summary>
public class HybridRepository<TBaseEvent, TSnapshot>
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    private readonly EventRepository<TBaseEvent, TSnapshot> _eventRepository;
    private readonly EventStore<TBaseEvent> _eventStore;
    private readonly IFeedIteratorFactory _feedIteratorFactory;
    private readonly EventStoreOptions _options;
    private readonly SnapshotRepository<TBaseEvent, TSnapshot> _snapshotRepository;
    private readonly Func<TBaseEvent, string> _snapshotPartitionKeyResolver;

    public HybridRepository(
        EventStore<TBaseEvent> eventStore,
        EventRepository<TBaseEvent, TSnapshot> eventRepository,
        SnapshotRepository<TBaseEvent, TSnapshot> snapshotRepository,
        SnapshotPartitionKeyResolverFactory snapshotPartitionKeyResolverFactory,
        IFeedIteratorFactory feedIteratorFactory,
        IOptions<EventStoreOptions> options
    )
    {
        _eventStore = eventStore;
        _eventRepository = eventRepository;
        _snapshotRepository = snapshotRepository;
        _snapshotPartitionKeyResolver = snapshotPartitionKeyResolverFactory.GetResolver<TBaseEvent, TSnapshot>();
        _feedIteratorFactory = feedIteratorFactory;
        _options = options.Value;
    }

    public async IAsyncEnumerable<TSnapshot?> QuerySnapshotsWithCatchupExpensivelyAsync(
        string partitionKey,
        Func<IQueryable<TSnapshot>, IQueryable<TSnapshot>> applyQuery,
        bool includeDeleted = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
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

                yield return await ApplyNewEvents(result, _options.EscapeIdIfRequired(result.Id), includeDeleted, cancellationToken);
            }
        }
    }

    public async Task<TSnapshot?> GetSnapshotWithCatchupExpensivelyAsync(
        string id,
        string partitionKey,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default
    )
    {
        var streamId = _options.EscapeIdIfRequired(id);
        var snapshot = await _snapshotRepository.GetSnapshot(id, partitionKey, includeDeleted, cancellationToken);

        return await ApplyNewEvents(snapshot, streamId, includeDeleted, cancellationToken);
    }

    public async Task<TSnapshot> ApplyEventsAndUpdateSnapshotImmediately(
        string id,
        int? expectedRevision,
        CancellationToken cancellationToken,
        params TBaseEvent[] events
    )
    {
        var snapshot = await _eventRepository.ApplyEvents(id, expectedRevision, cancellationToken, events);

        try
        {
            var partitionKey = _snapshotPartitionKeyResolver(events.First());
            var eventRevision = expectedRevision.GetValueOrDefault(0);
            var eventsToUpdate = events.Select(e => new Event<TBaseEvent>(id, eventRevision++, e)).ToList();

            await _snapshotRepository.ApplyEventsToSnapshot(id, partitionKey, eventsToUpdate, cancellationToken);
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
        CancellationToken cancellationToken = default
    )
    {
        var startPosition = snapshot?.Revision + 1 ?? 1;

        var eventStoreEvents = await _eventStore.ReadStreamForwards(streamId, startPosition, int.MaxValue, cancellationToken);

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
