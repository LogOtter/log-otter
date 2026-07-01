namespace LogOtter.CosmosDb.EventStore.Metadata;

internal record CompactionMetadata(Type CompactorType, Type SnapshotType);
