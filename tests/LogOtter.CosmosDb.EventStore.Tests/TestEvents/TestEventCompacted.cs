namespace LogOtter.CosmosDb.EventStore.Tests.TestEvents;

public class TestEventCompacted(string id, string scrubbedName) : TestEvent(id), ICompactionEvent
{
    public string ScrubbedName { get; } = scrubbedName;

    public override void Apply(TestEventProjection model, EventInfo eventInfo)
    {
        model.Name = ScrubbedName;
    }
}
