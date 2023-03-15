namespace LogOtter.CosmosDb.EventStore.Metadata;

public interface IProjectionMetadata
{
    Type EventType { get; }

    ISnapshotMetadata? SnapshotMetadata { get; }
}
