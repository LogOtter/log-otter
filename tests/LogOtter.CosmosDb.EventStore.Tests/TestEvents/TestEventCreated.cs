namespace LogOtter.CosmosDb.EventStore.Tests.TestEvents;

public class TestEventCreated(string id, string name) : TestEvent(id)
{
    public string Name { get; } = name;

    public override void Apply(TestEventProjection model, EventInfo eventInfo)
    {
        model.Name = Name;
    }
}
