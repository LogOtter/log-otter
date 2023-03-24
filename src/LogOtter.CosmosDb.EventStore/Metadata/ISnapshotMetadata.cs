using LogOtter.CosmosDb.Metadata;

namespace LogOtter.CosmosDb.EventStore.Metadata;

internal interface ISnapshotMetadata
{
    string ContainerName { get; }

    AutoProvisionMetadata? AutoProvisionMetadata { get; }
}
