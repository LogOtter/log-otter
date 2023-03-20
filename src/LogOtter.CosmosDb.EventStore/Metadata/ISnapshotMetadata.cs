namespace LogOtter.CosmosDb.EventStore.Metadata;

internal interface ISnapshotMetadata
{
    string ContainerName { get; }

    string PartitionKeyPath { get; }

    AutoProvisionMetadata? AutoProvisionMetadata { get; }
}
