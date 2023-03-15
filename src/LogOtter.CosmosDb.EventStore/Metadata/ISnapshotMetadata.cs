namespace LogOtter.CosmosDb.EventStore.Metadata;

public interface ISnapshotMetadata
{
    string ContainerName { get; }

    string PartitionKeyPath { get; }

    AutoProvisionMetadata? AutoProvisionMetadata { get; }
}
