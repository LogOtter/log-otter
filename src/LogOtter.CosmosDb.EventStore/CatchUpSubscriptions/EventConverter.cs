namespace LogOtter.CosmosDb.EventStore;

public class EventConverter<TBaseEvent>(EventStore<TBaseEvent> eventStore) : IChangeFeedChangeConverter<CosmosDbStorageEvent, Event<TBaseEvent>>
    where TBaseEvent : class
{
    public Event<TBaseEvent> ConvertChange(CosmosDbStorageEvent change)
    {
        var storageEvent = eventStore.FromCosmosStorageEvent(change);

        return Event<TBaseEvent>.FromStorageEvent(storageEvent);
    }
}
