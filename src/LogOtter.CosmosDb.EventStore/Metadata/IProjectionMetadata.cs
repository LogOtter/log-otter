namespace LogOtter.CosmosDb.EventStore.Metadata;

internal interface IProjectionMetadata
{
    Type EventType { get; }
    Type ProjectionType { get; }
    ISnapshotMetadata? SnapshotMetadata { get; }
}

internal interface IProjectionMetadata<TBaseEvent> : IProjectionMetadata { }
