namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;

public class Event(string id, string streamId, string bodyType, int eventNumber, Guid eventId, string description, DateTimeOffset timestamp)
{
    public string Id { get; } = id;
    public string StreamId { get; } = streamId;
    public string BodyType { get; } = bodyType;
    public int EventNumber { get; } = eventNumber;
    public Guid EventId { get; } = eventId;
    public string Description { get; } = description;
    public DateTimeOffset Timestamp { get; } = timestamp;
}
