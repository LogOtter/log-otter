using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore;

public class StreamCompactionService<TBaseEvent, TSnapshot>(
    EventStore<TBaseEvent> eventStore,
    SnapshotRepository<TBaseEvent, TSnapshot> snapshotRepository,
    IStreamCompactor<TBaseEvent, TSnapshot> compactor,
    IOptions<EventStoreOptions> options
)
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    private readonly EventStoreOptions _options = options.Value;

    public async Task CompactStream(string id, CancellationToken cancellationToken = default)
    {
        var streamId = _options.EscapeIdIfRequired(id);

        var existingEvents = await eventStore.ReadStreamForwards(streamId, cancellationToken);

        if (existingEvents.Count == 0)
        {
            return;
        }

        var firstEvent = existingEvents.First();

        EventData<TBaseEvent> tombstoneData;

        if (firstEvent.EventBody is ICompactionEvent)
        {
            // Resuming a previously-started compaction: the tombstone is already in place. Reuse it
            // verbatim — id, timestamp, metadata, body — rather than re-deriving the projection.
            // Events 2..N may have been partly deleted by the previous attempt, so a fresh projection
            // would be built from a truncated stream and produce a stale tombstone.
            tombstoneData = new EventData<TBaseEvent>(firstEvent.EventId, firstEvent.EventBody, firstEvent.CreatedOn, firstEvent.Metadata);
        }
        else
        {
            var projection = new TSnapshot { Id = streamId, Revision = existingEvents.Count };
            foreach (var @event in existingEvents)
            {
                @event.EventBody.Apply(projection, new EventInfo(@event.CreatedOn, @event.EventNumber, @event.Metadata));
            }

            var tombstone = compactor.CreateTombstoneEvent(projection, streamId);
            tombstoneData = new EventData<TBaseEvent>(Guid.NewGuid(), tombstone, DateTimeOffset.Now);
        }

        await eventStore.CompactStream(streamId, tombstoneData, cancellationToken);

        var compactedSnapshot = new TSnapshot { Id = streamId, Revision = 1 };
        tombstoneData.Body.Apply(compactedSnapshot, new EventInfo(tombstoneData.CreatedOn, 1, tombstoneData.Metadata));

        await snapshotRepository.UpsertSnapshot(compactedSnapshot, cancellationToken);
    }
}
