using System.ComponentModel;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore;

public class EventRepository<TBaseEvent, TSnapshot>(EventStore<TBaseEvent> eventStore, IOptions<EventStoreOptions> options)
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    private readonly EventStoreOptions _options = options.Value;

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

    public async Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, TBaseEvent firstEvent, params TBaseEvent[] additionalEvents)
    {
        return await ApplyEvents(id, expectedRevision, CancellationToken.None, [firstEvent, .. additionalEvents]);
    }

    public async Task<TSnapshot> ApplyEvents(
        string id,
        int? expectedRevision,
        CancellationToken cancellationToken,
        TBaseEvent firstEvent,
        params TBaseEvent[] additionalEvents
    )
    {
        return await ApplyEvents(id, expectedRevision, cancellationToken, [firstEvent, .. additionalEvents]);
    }

    public async Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, TBaseEvent[] events)
    {
        return await ApplyEvents(id, expectedRevision, CancellationToken.None, events.AsEnumerable());
    }

    public async Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, CancellationToken cancellationToken, TBaseEvent[] events)
    {
        return await ApplyEvents(id, expectedRevision, cancellationToken, events.AsEnumerable());
    }

    public async Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, IEnumerable<TBaseEvent> events)
    {
        return await ApplyEvents(id, expectedRevision, CancellationToken.None, events);
    }

    public async Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, CancellationToken cancellationToken, IEnumerable<TBaseEvent> events)
    {
        var (snapshot, _) = await ApplyAndGetEvents(id, expectedRevision, cancellationToken, events);
        return snapshot;
    }

    [Obsolete(EventArguments.AtLeastOneEventRequired, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task<TSnapshot> ApplyEvents(string id, int? expectedRevision) => throw new ArgumentException(EventArguments.AtLeastOneEventRequired);

    [Obsolete(EventArguments.AtLeastOneEventRequired, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, CancellationToken cancellationToken) =>
        throw new ArgumentException(EventArguments.AtLeastOneEventRequired);

    public async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(
        string id,
        int? expectedRevision,
        TBaseEvent firstEvent,
        params TBaseEvent[] additionalEvents
    )
    {
        return await ApplyAndGetEvents(id, expectedRevision, CancellationToken.None, [firstEvent, .. additionalEvents]);
    }

    public async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(
        string id,
        int? expectedRevision,
        CancellationToken cancellationToken,
        TBaseEvent firstEvent,
        params TBaseEvent[] additionalEvents
    )
    {
        return await ApplyAndGetEvents(id, expectedRevision, cancellationToken, [firstEvent, .. additionalEvents]);
    }

    public async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(string id, int? expectedRevision, TBaseEvent[] events)
    {
        return await ApplyAndGetEvents(id, expectedRevision, CancellationToken.None, events.AsEnumerable());
    }

    public async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(
        string id,
        int? expectedRevision,
        CancellationToken cancellationToken,
        TBaseEvent[] events
    )
    {
        return await ApplyAndGetEvents(id, expectedRevision, cancellationToken, events.AsEnumerable());
    }

    public async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(string id, int? expectedRevision, IEnumerable<TBaseEvent> events)
    {
        return await ApplyAndGetEvents(id, expectedRevision, CancellationToken.None, events);
    }

    public async Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(
        string id,
        int? expectedRevision,
        CancellationToken cancellationToken,
        IEnumerable<TBaseEvent> events
    )
    {
        var eventsToApply = EventArguments.ToNonEmptyListOrThrow(events);

        if (eventsToApply.Any(e => e.EventStreamId != id))
        {
            throw new ArgumentException("All events must be for the same entity", nameof(events));
        }

        var now = DateTimeOffset.Now;
        var streamId = _options.EscapeIdIfRequired(id);
        var entity = await Get(id, null, true, cancellationToken) ?? new TSnapshot { Id = streamId };
        var revision = entity.Revision;

        foreach (var eventToApply in eventsToApply)
        {
            eventToApply.Apply(entity, new(now, ++revision, new()));
        }

        var eventData = eventsToApply.Select(e => new EventData<TBaseEvent>(Guid.NewGuid(), e, now)).ToArray();

        await eventStore.AppendToStream(streamId, expectedRevision ?? 0, cancellationToken, eventData);

        entity.Revision = revision;

        return (entity, eventData);
    }

    // The Obsolete/non-EditorBrowsable overloads catch scenarios where a user doesn't pass at least one event (otherwise they'd
    // be picked up by the params overload)
    [Obsolete(EventArguments.AtLeastOneEventRequired, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(string id, int? expectedRevision) =>
        throw new ArgumentException(EventArguments.AtLeastOneEventRequired);

    [Obsolete(EventArguments.AtLeastOneEventRequired, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task<(TSnapshot, EventData<TBaseEvent>[])> ApplyAndGetEvents(string id, int? expectedRevision, CancellationToken cancellationToken) =>
        throw new ArgumentException(EventArguments.AtLeastOneEventRequired);
}
