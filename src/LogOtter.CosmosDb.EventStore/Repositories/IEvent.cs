namespace LogOtter.CosmosDb.EventStore;

public interface IEvent<TSnapshot>
    where TSnapshot : ISnapshot
{
    string EventStreamId { get; }
    void Apply(TSnapshot model);
}
