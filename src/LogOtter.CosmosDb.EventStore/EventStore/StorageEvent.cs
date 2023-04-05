namespace LogOtter.CosmosDb.EventStore;

public class StorageEvent
{
    public string StreamId { get; }

    public object EventBody { get; }

    public Dictionary<string, string> Metadata { get; }

    public int EventNumber { get; }

    public Guid EventId { get; }

    public DateTimeOffset CreatedOn { get; }

    public StorageEvent(string streamId, EventData data, int eventNumber)
    {
        StreamId = streamId;
        EventBody = data.Body;
        Metadata = data.Metadata;
        EventNumber = eventNumber;
        EventId = data.EventId;
        CreatedOn = data.CreatedOn;
    }
}
