namespace LogOtter.CosmosDb.EventStore.Tests;

public class MessageEvent : IEventualEvent<Message>
{
    public string EventId { get; }
    public string EventStreamId { get; }

    public DateTimeOffset Timestamp { get; }

    public string State { get; }

    public MessageEvent(string eventId, string eventStreamId, DateTimeOffset timestamp, string state)
    {
        EventId = eventId;
        EventStreamId = eventStreamId;
        Timestamp = timestamp;
        State = state;
    }

    public void Apply(Message model)
    {
        model.State = State;
    }
}
