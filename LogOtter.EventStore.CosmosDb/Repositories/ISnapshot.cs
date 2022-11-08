namespace LogOtter.EventStore.CosmosDb;

public interface ISnapshot
{
    int Revision { get; set; }
    string Id { get; init; }
    string PartitionKey { get; }
    DateTimeOffset? DeletedAt { get; set; }
}