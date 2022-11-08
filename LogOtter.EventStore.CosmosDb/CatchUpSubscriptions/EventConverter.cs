namespace LogOtter.EventStore.CosmosDb;

public class EventConverter<TBaseEvent> : IChangeFeedChangeConverter<CosmosDbStorageEvent, Event<TBaseEvent>>
{
    private readonly EventStore _eventStore;

    public EventConverter(EventStoreDependency<TBaseEvent> eventStoreDependency)
    {
        _eventStore = eventStoreDependency.EventStore;
    }

    public Event<TBaseEvent> ConvertChange(CosmosDbStorageEvent change)
    {
        var storageEvent = _eventStore.FromCosmosStorageEvent(change);

        return Event<TBaseEvent>.FromStorageEvent(storageEvent);
    }
}