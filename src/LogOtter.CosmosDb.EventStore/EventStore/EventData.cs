namespace LogOtter.CosmosDb.EventStore;

public class EventData<TBaseEvent>(Guid eventId, TBaseEvent body, DateTimeOffset createdOn, Dictionary<string, string>? metadata = null)
    where TBaseEvent : class
{
    public Guid EventId { get; } = eventId;

    public TBaseEvent Body { get; } = body;

    public Dictionary<string, string> Metadata { get; } = metadata ?? new Dictionary<string, string>();

    public DateTimeOffset CreatedOn { get; } = createdOn;
}
