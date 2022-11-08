namespace LogOtter.EventStore.CosmosDb;

public class EventRepository<TBaseEvent, TSnapshot>
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    private readonly EventStore _eventStore;

    public EventRepository(EventStoreDependency<TBaseEvent> eventStoreDependency)
    {
        _eventStore = eventStoreDependency.EventStore;
    }

    public async Task<TSnapshot?> Get(string id, int? revision = null, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var streamId = CosmosHelpers.EscapeForCosmosId(id);
        
        var eventStoreEvents = await _eventStore.ReadStreamForwards(streamId, cancellationToken);

        var eventsSelect = eventStoreEvents.Select(e => (TBaseEvent)e.EventBody);

        var events = revision != null
            ? eventsSelect.Take(revision.Value).ToList()
            : eventsSelect.ToList();

        if (!events.Any())
        {
            return null;
        }

        var model = new TSnapshot
        {
            Revision = events.Count,
            Id = streamId
        };

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

    public async Task<TSnapshot> ApplyEvents(string id, int? expectedRevision, params TBaseEvent[] events)
    {
        if (events.Any(e => e.EventStreamId != id))
        {
            throw new ArgumentException("All events must be for the same entity", nameof(events));
        }

        var entity = await Get(id, null, true) ?? new TSnapshot();

        foreach (var eventToApply in events)
        {
            eventToApply.Apply(entity);
        }

        var streamId = CosmosHelpers.EscapeForCosmosId(id);

        var eventData = events
            .Select(e => new EventData(Guid.NewGuid(), e, e.Ttl ?? -1))
            .ToArray();

        await _eventStore.AppendToStream(streamId, expectedRevision ?? 0, eventData);
        
        entity.Revision += events.Length;
        
        return entity;
    }
}