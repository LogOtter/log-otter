namespace LogOtter.CosmosDb.EventStore;

public class Event<TBaseEvent>
{
    public string StreamId { get; }
    public int EventNumber { get; }
    public TBaseEvent Body { get; }

    public Event(string streamId, int eventNumber, TBaseEvent body)
    {
        Body = body;
        EventNumber = eventNumber;
        StreamId = streamId;
    }

    public static Event<TBaseEvent> FromStorageEvent(StorageEvent storageEvent)
    {
        return new(
            storageEvent.StreamId,
            storageEvent.EventNumber,
            (TBaseEvent)storageEvent.EventBody
        );
    }
}