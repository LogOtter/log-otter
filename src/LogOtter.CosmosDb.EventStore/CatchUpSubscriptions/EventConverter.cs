namespace LogOtter.CosmosDb.EventStore;

public class EventConverter<TBaseEvent> : IChangeFeedChangeConverter<StorageEvent<TBaseEvent>, Event<TBaseEvent>>
    where TBaseEvent : class
{
    public Event<TBaseEvent> ConvertChange(StorageEvent<TBaseEvent> change) => Event<TBaseEvent>.FromStorageEvent(change);
}
