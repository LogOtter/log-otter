namespace LogOtter.CosmosDb.EventStore;

public class EventualStorageEvent<TBaseEvent> : IEventualStorageEvent
    where TBaseEvent : class
{
    public Type BaseEventType => typeof(TBaseEvent);

    object IEventualStorageEvent.EventBody => EventBody;

    public string StreamId { get; }

    public TBaseEvent EventBody { get; }

    public Dictionary<string, string> Metadata { get; }

    public DateTimeOffset Timestamp { get; }

    public Guid EventId { get; }

    public DateTimeOffset CreatedOn { get; }

    public EventualStorageEvent(string streamId, EventualEventData<TBaseEvent> data)
    {
        StreamId = streamId;
        EventBody = data.Body;
        Metadata = data.Metadata;
        Timestamp = data.Timestamp;
        EventId = data.EventId;
        CreatedOn = data.CreatedOn;
    }
}
