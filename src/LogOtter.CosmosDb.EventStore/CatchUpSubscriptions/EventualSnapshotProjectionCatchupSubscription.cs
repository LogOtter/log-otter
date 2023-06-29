using LogOtter.CosmosDb.EventStore.Metadata;
using Microsoft.Extensions.Logging;

namespace LogOtter.CosmosDb.EventStore;

internal class EventualSnapshotProjectionCatchupSubscription<TBaseEvent, TSnapshot> : IEventualCatchupSubscription<TBaseEvent>
    where TBaseEvent : class, IEventualEvent<TSnapshot>
    where TSnapshot : class, IEventualSnapshot, new()
{
    private readonly ILogger<EventualSnapshotProjectionCatchupSubscription<TBaseEvent, TSnapshot>> _logger;
    private readonly EventualSnapshotRepository<TBaseEvent, TSnapshot> _snapshotRepository;
    private readonly Func<TBaseEvent, string> _snapshotPartitionKeyResolver;

    public EventualSnapshotProjectionCatchupSubscription(
        EventualSnapshotRepository<TBaseEvent, TSnapshot> snapshotRepository,
        ILogger<EventualSnapshotProjectionCatchupSubscription<TBaseEvent, TSnapshot>> logger,
        SnapshotPartitionKeyResolverFactory snapshotPartitionKeyResolverFactory
    )
    {
        _snapshotRepository = snapshotRepository;
        _logger = logger;
        _snapshotPartitionKeyResolver = snapshotPartitionKeyResolverFactory.GetResolver<TBaseEvent, TSnapshot>();
    }

    public async Task ProcessEvents(IReadOnlyCollection<EventualEvent<TBaseEvent>> events, CancellationToken cancellationToken)
    {
        var eventIds = events.Select(e => (StreamId: e.StreamId, PartitionKey: _snapshotPartitionKeyResolver(e.Body)));

        foreach (var eventId in eventIds)
        {
            try
            {
                await _snapshotRepository.UpdateSnapshot(eventId.StreamId, eventId.PartitionKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eventual Snapshot Projection: Error applying events to snapshot for stream ID {StreamId}", eventId.StreamId);
                throw;
            }
        }
    }
}
