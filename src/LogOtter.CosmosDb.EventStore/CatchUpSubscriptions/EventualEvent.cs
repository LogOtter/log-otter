namespace LogOtter.CosmosDb.EventStore;

public class EventualEvent<TBaseEvent>
    where TBaseEvent : class
{
    public string StreamId { get; }
    public DateTimeOffset Timestamp { get; }
    public TBaseEvent Body { get; }

    public EventualEvent(string streamId, DateTimeOffset timestamp, TBaseEvent body)
    {
        Body = body;
        Timestamp = timestamp;
        StreamId = streamId;
    }

    public static EventualEvent<TBaseEvent> FromStorageEvent(EventualStorageEvent<TBaseEvent> storageEvent)
    {
        return new EventualEvent<TBaseEvent>(storageEvent.StreamId, storageEvent.Timestamp, (TBaseEvent)storageEvent.EventBody);
    }
}
