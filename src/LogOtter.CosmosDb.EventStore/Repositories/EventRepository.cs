using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore;

public class EventRepository<TBaseEvent, TSnapshot>(
    EventStore<TBaseEvent> eventStore,
    IOptions<EventStoreOptions> options,
    IEnumerable<IEventMetadataEnricher>? metadataEnrichers = null
)
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    private readonly EventStoreOptions _options = options.Value;
    private readonly IReadOnlyCollection<IEventMetadataEnricher> _metadataEnrichers = (
        metadataEnrichers ?? Array.Empty<IEventMetadataEnricher>()
    ).ToArray();

    public async Task<TSnapshot?> Get(string id, int? revision = null, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var streamId = _options.EscapeIdIfRequired(id);

        var eventStoreEvents = await eventStore.ReadStreamForwards(streamId, cancellationToken);

        var eventsSelect = eventStoreEvents.Select(e => e);

        var events = revision != null ? eventsSelect.Take(revision.Value).ToList() : eventsSelect.ToList();

        if (!events.Any())
        {
            return null;
        }

        var model = new TSnapshot { Revision = events.Count, Id = streamId };

        foreach (var @event in events)
        {
            @event.EventBody.Apply(model, new(@event.CreatedOn, @event.EventNumber, @event.Metadata));
        }

        if (model.DeletedAt.HasValue && !includeDeleted)
        {
            return null;
        }

        return model;
    }

    public async Task<IReadOnlyCollection<TBaseEvent>> GetEventStream(string id, CancellationToken cancellationToken = default)
    {
        var streamId = _options.EscapeIdIfRequired(id);
        var eventStoreEvents = await eventStore.ReadStreamForwards(streamId, cancellationToken);

        var events = eventStoreEvents.Select(e => (TBaseEvent)e.EventBody).ToList();

        return events;
    }

    public async Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, params TBaseEvent[] events)
    {
        return await ApplyEvents(id, expectedRevision, CancellationToken.None, events);
    }

    public async Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, CancellationToken cancellationToken, params TBaseEvent[] events)
    {
        var (entity, _) = await ApplyEventsInternal(id, expectedRevision, null, cancellationToken, events);
        return entity;
    }

    public async Task<TSnapshot> ApplyEvents(
        string id,
        int? expectedRevision,
        IReadOnlyDictionary<string, string>? additionalMetadata,
        params TBaseEvent[] events
    )
    {
        return await ApplyEvents(id, expectedRevision, additionalMetadata, CancellationToken.None, events);
    }

    public async Task<TSnapshot> ApplyEvents(
        string id,
        int? expectedRevision,
        IReadOnlyDictionary<string, string>? additionalMetadata,
        CancellationToken cancellationToken,
        params TBaseEvent[] events
    )
    {
        var (entity, _) = await ApplyEventsInternal(id, expectedRevision, additionalMetadata, cancellationToken, events);
        return entity;
    }

    public async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(string id, int? expectedRevision, params TBaseEvent[] events)
    {
        return await ApplyAndGetEvents(id, expectedRevision, CancellationToken.None, events);
    }

    public async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(
        string id,
        int? expectedRevision,
        CancellationToken cancellationToken,
        params TBaseEvent[] events
    )
    {
        return await ApplyEventsInternal(id, expectedRevision, null, cancellationToken, events);
    }

    public async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(
        string id,
        int? expectedRevision,
        IReadOnlyDictionary<string, string>? additionalMetadata,
        params TBaseEvent[] events
    )
    {
        return await ApplyAndGetEvents(id, expectedRevision, additionalMetadata, CancellationToken.None, events);
    }

    public async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(
        string id,
        int? expectedRevision,
        IReadOnlyDictionary<string, string>? additionalMetadata,
        CancellationToken cancellationToken,
        params TBaseEvent[] events
    )
    {
        return await ApplyEventsInternal(id, expectedRevision, additionalMetadata, cancellationToken, events);
    }

    private async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyEventsInternal(
        string id,
        int? expectedRevision,
        IReadOnlyDictionary<string, string>? additionalMetadata,
        CancellationToken cancellationToken,
        TBaseEvent[] events
    )
    {
        if (events.Any(e => e.EventStreamId != id))
        {
            throw new ArgumentException("All events must be for the same entity", nameof(events));
        }

        var now = DateTimeOffset.Now;
        var streamId = _options.EscapeIdIfRequired(id);
        var entity = await Get(id, null, true, cancellationToken) ?? new TSnapshot { Id = streamId };
        var revision = entity.Revision;

        // Build once per batch: every event in the batch shares the same metadata snapshot as they
        // all originate from the same request/trace. The dictionary is not mutated after this point,
        // so the reference can be shared safely across the events and the in-memory apply.
        var metadata = BuildMetadata(additionalMetadata);

        foreach (var eventToApply in events)
        {
            eventToApply.Apply(entity, new(now, ++revision, metadata));
        }

        var eventData = events.Select(e => new EventData<TBaseEvent>(Guid.NewGuid(), e, now, metadata)).ToArray();

        await eventStore.AppendToStream(streamId, expectedRevision ?? 0, cancellationToken, eventData);

        entity.Revision = revision;

        return (entity, eventData);
    }

    private Dictionary<string, string> BuildMetadata(IReadOnlyDictionary<string, string>? additionalMetadata)
    {
        var metadata = new Dictionary<string, string>();

        foreach (var enricher in _metadataEnrichers)
        {
            enricher.Enrich(metadata);
        }

        if (additionalMetadata != null)
        {
            // Caller-supplied values win on key collision with enricher output.
            foreach (var entry in additionalMetadata)
            {
                metadata[entry.Key] = entry.Value;
            }
        }

        return metadata;
    }
}
