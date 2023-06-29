namespace LogOtter.CosmosDb.EventStore;

public class EventualEventConverter<TBaseEvent> : IChangeFeedChangeConverter<CosmosDbEventualStorageEvent, EventualEvent<TBaseEvent>>
    where TBaseEvent : class
{
    private readonly EventualEventStore<TBaseEvent> _eventStore;

    public EventualEventConverter(EventualEventStore<TBaseEvent> eventStore)
    {
        _eventStore = eventStore;
    }

    public EventualEvent<TBaseEvent> ConvertChange(CosmosDbEventualStorageEvent change)
    {
        var storageEvent = _eventStore.FromCosmosStorageEvent(change);

        return EventualEvent<TBaseEvent>.FromStorageEvent(storageEvent);
    }
}
