namespace LogOtter.CosmosDb.EventStore;

internal record CompactionMetadata(Type CompactorType, Type SnapshotType);
