using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore;

public class EventualEventRepository<TBaseEvent, TSnapshot>
    where TBaseEvent : class, IEventualEvent<TSnapshot>
    where TSnapshot : class, IEventualSnapshot, new()
{
    private readonly EventualEventStore<TBaseEvent> _eventStore;
    private readonly EventStoreOptions _options;

    public EventualEventRepository(EventualEventStore<TBaseEvent> eventStore, IOptions<EventStoreOptions> options)
    {
        _eventStore = eventStore;
        _options = options.Value;
    }

    public async Task<TSnapshot?> Get(string id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var streamId = _options.EscapeIdIfRequired(id);

        var eventStoreEvents = await _eventStore.ReadStreamForwards(streamId, cancellationToken);

        var events = eventStoreEvents.Select(e => e.EventBody).ToList();

        if (!events.Any())
        {
            return null;
        }

        var model = new TSnapshot { Id = streamId };

        foreach (var @event in events)
        {
            @event.Apply(model);
        }

        if (model.DeletedAt.HasValue && !includeDeleted)
        {
            return null;
        }

        return model;
    }

    public async Task<TSnapshot> ApplyEvents(string id, params TBaseEvent[] events)
    {
        return await ApplyEvents(id, CancellationToken.None, events);
    }

    public async Task<TSnapshot> ApplyEvents(string id, CancellationToken cancellationToken, params TBaseEvent[] events)
    {
        if (events.Any(e => e.EventStreamId != id))
        {
            throw new ArgumentException("All events must be for the same entity", nameof(events));
        }

        var streamId = _options.EscapeIdIfRequired(id);

        var now = DateTimeOffset.Now;
        var eventData = events.Select(e => new EventualEventData<TBaseEvent>(e.EventId, e, e.Timestamp, now)).ToArray();

        await _eventStore.AddToStream(streamId, cancellationToken, eventData);

        return await Get(id, true, cancellationToken) ?? new TSnapshot();
    }
}
