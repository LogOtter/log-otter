namespace LogOtter.CosmosDb.EventStore;

public interface ISnapshottableEvent<TSnapshot>
    : IEvent<TSnapshot>
    where TSnapshot : ISnapshot
{
    string SnapshotPartitionKey { get; }
}