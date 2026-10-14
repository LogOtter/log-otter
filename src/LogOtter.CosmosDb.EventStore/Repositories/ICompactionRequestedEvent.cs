namespace LogOtter.CosmosDb.EventStore;

/// <summary>
///  Marks the event stream as requesting a compaction, the Compaction CFP will attempt to resolve and execute the compaction against the event stream
/// </summary>
public interface ICompactionRequestedEvent<TSnapshot> : IEvent<TSnapshot>
    where TSnapshot : ISnapshot;
