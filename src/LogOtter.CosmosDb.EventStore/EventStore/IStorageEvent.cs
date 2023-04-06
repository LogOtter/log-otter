namespace LogOtter.CosmosDb.EventStore;

public interface IStorageEvent
{
    string StreamId { get; }
    object EventBody { get; }
    Dictionary<string, string> Metadata { get; }
    int EventNumber { get; }
    Guid EventId { get; }
    DateTimeOffset CreatedOn { get; }
    Type BaseEventType { get; }
}
