namespace LogOtter.CosmosDb.EventStore.Tests.TestEvents;

public class TestEventModified(string id, string newName) : TestEvent(id)
{
    public string NewName { get; } = newName;

    public override void Apply(TestEventProjection model, EventInfo eventInfo)
    {
        model.Name = NewName;
    }
}
