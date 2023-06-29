namespace LogOtter.CosmosDb.EventStore;

public interface IEventualSnapshot
{
    DateTimeOffset LastUpdated { get; set; }
    string Id { get; init; }
    string PartitionKey { get; }
    DateTimeOffset? DeletedAt { get; set; }
}
