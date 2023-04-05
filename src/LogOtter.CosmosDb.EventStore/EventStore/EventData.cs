namespace LogOtter.CosmosDb.EventStore;

public class EventData
{
    public Guid EventId { get; }

    public object Body { get; }

    public Dictionary<string, string> Metadata { get; }

    public DateTimeOffset CreatedOn { get; }

    public EventData(Guid eventId, object body, DateTimeOffset createdOn, Dictionary<string, string>? metadata = null)
    {
        EventId = eventId;
        Body = body;
        CreatedOn = createdOn;
        Metadata = metadata ?? new Dictionary<string, string>();
    }
}
