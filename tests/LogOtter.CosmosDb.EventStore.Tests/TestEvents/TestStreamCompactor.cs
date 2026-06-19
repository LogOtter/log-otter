namespace LogOtter.CosmosDb.EventStore.Tests.TestEvents;

public class TestStreamCompactor : IStreamCompactor<TestEvent, TestEventProjection>
{
    public TestEvent CreateTombstoneEvent(TestEventProjection currentProjection, string streamId)
    {
        return new TestEventCompacted(streamId, "[REDACTED]");
    }
}
