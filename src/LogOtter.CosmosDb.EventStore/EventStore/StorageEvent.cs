namespace LogOtter.CosmosDb.EventStore;

public interface IStorageEvent
{
    string StreamId { get; }
    object EventBody { get; }
    Dictionary<string, string> Metadata { get; }
    int EventNumber { get; }
    Guid EventId { get; }
    DateTimeOffset CreatedOn { get; }
    Type BaseEventType { get; }
}

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

    public StorageEvent(string streamId, EventData<TBaseEvent> data, int eventNumber, DateTimeOffset createdOn)
    {
        StreamId = streamId;
        EventBody = data.Body;
        Metadata = data.Metadata;
        EventNumber = eventNumber;
        EventId = data.EventId;
        CreatedOn = createdOn;
    }
}
