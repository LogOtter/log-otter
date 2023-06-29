namespace LogOtter.CosmosDb.EventStore;

public interface IEventualEvent<TSnapshot>
    where TSnapshot : IEventualSnapshot
{
    string EventStreamId { get; }
    DateTimeOffset Timestamp { get; }
    void Apply(TSnapshot model);
}
