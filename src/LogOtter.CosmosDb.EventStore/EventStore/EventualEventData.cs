namespace LogOtter.CosmosDb.EventStore;

public class EventualEventData<TBaseEvent>
    where TBaseEvent : class
{
    public Guid EventId { get; }

    public TBaseEvent Body { get; }

    public Dictionary<string, string> Metadata { get; }

    public DateTimeOffset Timestamp { get; }

    public DateTimeOffset CreatedOn { get; }

    public EventualEventData(
        Guid eventId,
        TBaseEvent body,
        DateTimeOffset timestamp,
        DateTimeOffset createdOn,
        Dictionary<string, string>? metadata = null
    )
    {
        EventId = eventId;
        Body = body;
        Timestamp = timestamp;
        CreatedOn = createdOn;
        Metadata = metadata ?? new Dictionary<string, string>();
    }
}
