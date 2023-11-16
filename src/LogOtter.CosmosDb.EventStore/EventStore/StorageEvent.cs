namespace LogOtter.CosmosDb.EventStore;

public class StorageEvent<TBaseEvent>(string streamId, EventData<TBaseEvent> data, int eventNumber) : IStorageEvent
    where TBaseEvent : class
{
    public Type BaseEventType => typeof(TBaseEvent);

    object IStorageEvent.EventBody => EventBody;

    public string StreamId { get; } = streamId;

    public TBaseEvent EventBody { get; } = data.Body;

    public Dictionary<string, string> Metadata { get; } = data.Metadata;

    public int EventNumber { get; } = eventNumber;

    public Guid EventId { get; } = data.EventId;

    public DateTimeOffset CreatedOn { get; } = data.CreatedOn;
}
