namespace LogOtter.CosmosDb.EventStore;

public class EventData<TBaseEvent>
    where TBaseEvent : class
{
    public Guid EventId { get; }

    public TBaseEvent Body { get; }

    public Dictionary<string, string> Metadata { get; }

    public EventData(Guid eventId, TBaseEvent body, Dictionary<string, string>? metadata = null)
    {
        EventId = eventId;
        Body = body;
        Metadata = metadata ?? new Dictionary<string, string>();
    }
}
