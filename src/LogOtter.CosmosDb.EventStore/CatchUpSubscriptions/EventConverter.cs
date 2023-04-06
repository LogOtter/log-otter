namespace LogOtter.CosmosDb.EventStore;

public class EventConverter<TBaseEvent> : IChangeFeedChangeConverter<CosmosDbStorageEvent, Event<TBaseEvent>>
    where TBaseEvent : class
{
    private readonly EventStore<TBaseEvent> _eventStore;

    public EventConverter(EventStore<TBaseEvent> eventStore)
    {
        _eventStore = eventStore;
    }

    public Event<TBaseEvent> ConvertChange(CosmosDbStorageEvent change)
    {
        var storageEvent = _eventStore.FromCosmosStorageEvent(change);

        return Event<TBaseEvent>.FromStorageEvent(storageEvent);
    }
}
