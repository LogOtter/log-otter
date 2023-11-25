namespace LogOtter.CosmosDb.EventStore.Tests.TestEvents;

public abstract class TestEvent(string id) : IEvent<TestEventProjection>
{
    public string Id { get; } = id;
    public string EventStreamId => Id;

    public abstract void Apply(TestEventProjection model);
}
