namespace LogOtter.CosmosDb.EventStore;

public interface IEventualStorageEvent
{
    string StreamId { get; }
    object EventBody { get; }
    Dictionary<string, string> Metadata { get; }
    DateTimeOffset Timestamp { get; }
    Guid EventId { get; }
    DateTimeOffset CreatedOn { get; }
    Type BaseEventType { get; }
}
