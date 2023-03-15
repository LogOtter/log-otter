namespace LogOtter.CosmosDb.EventStore;

public class EventData
{
    public Guid EventId { get; }

    public object Body { get; }

    public int TimeToLive { get; }

    public object? Metadata { get; }

    public EventData(Guid eventId, object body, int timeToLive = -1, object? metadata = null)
    {
        EventId = eventId;
        Body = body;
        TimeToLive = timeToLive;
        Metadata = metadata;
    }
}
