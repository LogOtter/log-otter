using LogOtter.CosmosDb.EventStore.Metadata;
using Microsoft.Extensions.Logging;

namespace LogOtter.CosmosDb.EventStore;

internal class SnapshotProjectionCatchupSubscription<TBaseEvent, TSnapshot>(
    SnapshotRepository<TBaseEvent, TSnapshot> snapshotRepository,
    ILogger<SnapshotProjectionCatchupSubscription<TBaseEvent, TSnapshot>> logger,
    SnapshotPartitionKeyResolverFactory snapshotPartitionKeyResolverFactory
) : ICatchupSubscription<TBaseEvent>
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    private readonly Func<TBaseEvent, string> _snapshotPartitionKeyResolver = snapshotPartitionKeyResolverFactory.GetResolver<
        TBaseEvent,
        TSnapshot
    >();

    public async Task ProcessEvents(IReadOnlyCollection<Event<TBaseEvent>> events, CancellationToken cancellationToken)
    {
        var eventsByStream = events.GroupBy(e => e.StreamId);
        foreach (var eventsForStream in eventsByStream)
        {
            try
            {
                await ApplyEventsToSingleSnapshot(eventsForStream, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Snapshot Projection: Error applying events to snapshot for stream ID {StreamId}. Starting revision was {StartRevision}. Attempting to update to {EndRevision}",
                    eventsForStream.Key,
                    eventsForStream.Min(e => e.EventNumber),
                    eventsForStream.Max(e => e.EventNumber)
                );
                throw;
            }
        }
    }

    private async Task ApplyEventsToSingleSnapshot(IGrouping<string, Event<TBaseEvent>> events, CancellationToken cancellationToken)
    {
        var partitionKey = _snapshotPartitionKeyResolver(events.First().Body);
        var streamId = events.Key;

        await snapshotRepository.ApplyEventsToSnapshot(streamId, partitionKey, events.ToList(), cancellationToken);
    }
}
