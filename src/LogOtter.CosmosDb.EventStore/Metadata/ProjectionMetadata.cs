namespace LogOtter.CosmosDb.EventStore.Metadata;

internal record ProjectionMetadata<TBaseEvent, TProjection>(SnapshotMetadata<TBaseEvent, TProjection>? SnapshotMetadata = null)
    : IProjectionMetadata<TBaseEvent>
{
    Type IProjectionMetadata.EventType => typeof(TBaseEvent);
    Type IProjectionMetadata.ProjectionType => typeof(TProjection);

    ISnapshotMetadata? IProjectionMetadata.SnapshotMetadata => SnapshotMetadata;
}
