namespace LogOtter.EventStore.CosmosDb;

public interface ISnapshottableEvent<TSnapshot>
    : IEvent<TSnapshot>
    where TSnapshot : ISnapshot
{
    string SnapshotPartitionKey { get; }
}