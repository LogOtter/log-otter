namespace LogOtter.CosmosDb.EventStore;

public class Event<TBaseEvent>(string streamId, int eventNumber, TBaseEvent body, DateTimeOffset createdOn, Dictionary<string, string> metadata)
    where TBaseEvent : class
{
    public string StreamId { get; } = streamId;
    public int EventNumber { get; } = eventNumber;
    public TBaseEvent Body { get; } = body;
    public DateTimeOffset CreatedOn { get; } = createdOn;
    public Dictionary<string, string> Metadata { get; } = metadata;

    public static Event<TBaseEvent> FromStorageEvent(StorageEvent<TBaseEvent> storageEvent)
    {
        return new Event<TBaseEvent>(
            storageEvent.StreamId,
            storageEvent.EventNumber,
            (TBaseEvent)storageEvent.EventBody,
            storageEvent.CreatedOn,
            storageEvent.Metadata
        );
    }
}
