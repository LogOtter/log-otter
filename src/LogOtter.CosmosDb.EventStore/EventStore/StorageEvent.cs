namespace LogOtter.CosmosDb.EventStore;

public class StorageEvent<TBaseEvent> : IStorageEvent
    where TBaseEvent : class
{
    public Type BaseEventType => typeof(TBaseEvent);

    object IStorageEvent.EventBody => EventBody;

    public string StreamId { get; }

    public TBaseEvent EventBody { get; }

    public Dictionary<string, string> Metadata { get; }

    public int EventNumber { get; }

    public Guid EventId { get; }

    public DateTimeOffset CreatedOn { get; }

    public StorageEvent(string streamId, EventData<TBaseEvent> data, int eventNumber)
    {
        StreamId = streamId;
        EventBody = data.Body;
        Metadata = data.Metadata;
        EventNumber = eventNumber;
        EventId = data.EventId;
        CreatedOn = data.CreatedOn;
    }
}
