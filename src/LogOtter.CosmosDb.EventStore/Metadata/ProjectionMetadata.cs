namespace LogOtter.CosmosDb.EventStore.Metadata;

public record ProjectionMetadata<TBaseEvent, TProjection>
    (SnapshotMetadata<TBaseEvent, TProjection>? SnapshotMetadata = null) : IProjectionMetadata<TBaseEvent>
{
    Type IProjectionMetadata.EventType => typeof(TProjection);
    Type IProjectionMetadata<TBaseEvent>.ProjectionType => typeof(TProjection);

    ISnapshotMetadata? IProjectionMetadata.SnapshotMetadata => SnapshotMetadata;
}
