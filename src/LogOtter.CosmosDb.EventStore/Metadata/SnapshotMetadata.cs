using LogOtter.CosmosDb.Metadata;

namespace LogOtter.CosmosDb.EventStore.Metadata;

internal record SnapshotMetadata<TBaseEvent, TProjection>(
    string ContainerName,
    Func<TBaseEvent, string> SnapshotPartitionKeyResolver,
    AutoProvisionMetadata? AutoProvisionMetadata = null
) : ISnapshotMetadata;
