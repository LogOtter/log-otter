namespace LogOtter.CosmosDb.EventStore;

public interface IStreamCompactor<TBaseEvent, TSnapshot>
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    TBaseEvent CreateTombstoneEvent(TSnapshot currentProjection, string streamId);
}
