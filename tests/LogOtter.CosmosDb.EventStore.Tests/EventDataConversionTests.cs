using LogOtter.CosmosDb.EventStore.Tests.TestEvents;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class EventDataConversionTests
{
    [Fact]
    public void WrapperEventIsCreatedFromStorageEvent()
    {
        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";

        var testEvent = new TestEventCreated(id, name);
        var eventId = Guid.NewGuid();

        var createdOn = DateTimeOffset.UtcNow;
        var eventData = new EventData<TestEvent>(eventId, testEvent, createdOn, new Dictionary<string, string> { { "key1", "value1" } });
        var storageEvent = new StorageEvent<TestEvent>(id, eventData, 3);
        var wrapperEvent = Event<TestEvent>.FromStorageEvent(storageEvent);

        wrapperEvent.StreamId.ShouldBe(id);
        wrapperEvent.EventNumber.ShouldBe(3);
        wrapperEvent.CreatedOn.ShouldBe(createdOn);
        wrapperEvent.Metadata.ShouldContainKey("key1");
        wrapperEvent.Metadata["key1"].ShouldBe("value1");
    }
}
