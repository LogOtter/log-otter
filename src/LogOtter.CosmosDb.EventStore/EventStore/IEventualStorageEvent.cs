namespace LogOtter.CosmosDb.EventStore;

public interface IEventualStorageEvent
{
    string StreamId { get; }
    object EventBody { get; }
    Dictionary<string, string> Metadata { get; }
    DateTimeOffset Timestamp { get; }
    string EventId { get; }
    DateTimeOffset CreatedOn { get; }
    Type BaseEventType { get; }
}
