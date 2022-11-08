namespace LogOtter.EventStore.CosmosDb;

public interface IEvent<TSnapshot>
    where TSnapshot : ISnapshot
{
    string EventStreamId { get; }
    int? Ttl { get; }
    void Apply(TSnapshot model);
}