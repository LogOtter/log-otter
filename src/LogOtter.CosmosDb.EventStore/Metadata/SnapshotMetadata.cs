namespace LogOtter.CosmosDb.EventStore.Metadata;

internal record SnapshotMetadata<TBaseEvent, TProjection>(
    string ContainerName,
    string PartitionKeyPath,
    Func<TBaseEvent, string> SnapshotPartitionKeyResolver,
    AutoProvisionMetadata? AutoProvisionMetadata = null) : ISnapshotMetadata;
