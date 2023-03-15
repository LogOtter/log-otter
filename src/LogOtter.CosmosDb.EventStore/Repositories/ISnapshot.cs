namespace LogOtter.CosmosDb.EventStore;

public interface ISnapshot
{
    int Revision { get; set; }
    string Id { get; init; }
    string PartitionKey { get; }
    DateTimeOffset? DeletedAt { get; set; }
}
