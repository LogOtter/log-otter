namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;

internal class Event
{
    public string Id { get; }
    public string StreamId { get; }
    public string BodyType { get; }
    public int EventNumber { get; }
    public Guid EventId { get; }

    public int TimeToLive { get; }
    public string Description { get; }
    public DateTimeOffset Timestamp { get; }

    public Event(string id, string streamId, string bodyType, int eventNumber, Guid eventId, string description, DateTimeOffset timestamp)
    {
        Id = id;
        StreamId = streamId;
        BodyType = bodyType;
        EventNumber = eventNumber;
        EventId = eventId;
        Description = description;
        Timestamp = timestamp;
    }
}
