namespace LogOtter.CosmosDb.EventStore;

/// <summary>
/// Marks an event as a stream-compaction tombstone. When a read path encounters one of these,
/// it applies it and stops — any subsequent events in the stream are pending deletion and must
/// not contribute to the projection.
/// </summary>
public interface ICompactionEvent;
