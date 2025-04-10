using LogOtter.CosmosDb.EventStore.Tests.TestEvents;
using LogOtter.CosmosDb.Testing;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class EventStoreTests
{
    [Fact]
    public async Task SingleEventIsStoredCorrectly()
    {
        var eventStore = CreateEventStore();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";

        var testEvent = new TestEventCreated(id, name);
        var eventId = Guid.NewGuid();

        var eventData = new EventData<TestEvent>(eventId, testEvent, DateTimeOffset.UtcNow);

        await eventStore.AppendToStream(id, 0, eventData);

        var allEvents = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        allEvents.Count.ShouldBe(1);

        var createdEvent = allEvents.First();
        createdEvent.StreamId.ShouldBe(id);
        createdEvent.EventId.ShouldBe(eventId);
        createdEvent.EventBody.ShouldBeOfType<TestEventCreated>().Name.ShouldBe(name);
    }

    [Fact]
    public async Task MultipleEventsAreStoredCorrectly()
    {
        var eventStore = CreateEventStore();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";
        var newName = "Bobby Bobertson";

        var testCreatedEvent = new TestEventCreated(id, name);
        var testModifiedEvent = new TestEventModified(id, newName);
        var createdEventId = Guid.NewGuid();
        var modifiedEventId = Guid.NewGuid();
        var createdEventData = new EventData<TestEvent>(createdEventId, testCreatedEvent, DateTimeOffset.UtcNow);
        var modifiedEventData = new EventData<TestEvent>(modifiedEventId, testModifiedEvent, DateTimeOffset.UtcNow);

        await eventStore.AppendToStream(id, 0, createdEventData, modifiedEventData);

        var allEvents = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        allEvents.Count.ShouldBe(2);

        var createdEvent = allEvents.First();
        createdEvent.StreamId.ShouldBe(id);
        createdEvent.EventId.ShouldBe(createdEventId);
        createdEvent.EventBody.ShouldBeOfType<TestEventCreated>().Name.ShouldBe(name);

        var modifiedEvent = allEvents.Skip(1).First();
        modifiedEvent.StreamId.ShouldBe(id);
        modifiedEvent.EventId.ShouldBe(modifiedEventId);
        modifiedEvent.EventBody.ShouldBeOfType<TestEventModified>().NewName.ShouldBe(newName);
    }

    [Fact]
    public async Task MultipleEventsArePagedCorrectly()
    {
        var eventStore = CreateEventStore();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";
        var newName = "Bobby Bobertson";

        var testCreatedEvent = new TestEventCreated(id, name);
        var testModifiedEvent = new TestEventModified(id, newName);
        var createdEventId = Guid.NewGuid();
        var modifiedEventId = Guid.NewGuid();
        var createdEventData = new EventData<TestEvent>(createdEventId, testCreatedEvent, DateTimeOffset.UtcNow);
        var modifiedEventData = new EventData<TestEvent>(modifiedEventId, testModifiedEvent, DateTimeOffset.UtcNow);

        await eventStore.AppendToStream(id, 0, createdEventData, modifiedEventData);

        var pagedEvents = await eventStore.ReadStreamForwards(id, 1, 1, cancellationToken: TestContext.Current.CancellationToken);
        pagedEvents.Count.ShouldBe(1);

        var createdEvent = pagedEvents.First();
        createdEvent.StreamId.ShouldBe(id);
        createdEvent.EventId.ShouldBe(createdEventId);
        createdEvent.EventBody.ShouldBeOfType<TestEventCreated>().Name.ShouldBe(name);

        pagedEvents = await eventStore.ReadStreamForwards(id, 2, 1, cancellationToken: TestContext.Current.CancellationToken);
        pagedEvents.Count.ShouldBe(1);

        var modifiedEvent = pagedEvents.First();
        modifiedEvent.StreamId.ShouldBe(id);
        modifiedEvent.EventId.ShouldBe(modifiedEventId);
        modifiedEvent.EventBody.ShouldBeOfType<TestEventModified>().NewName.ShouldBe(newName);
    }

    [Fact]
    public async Task ThrowsExceptionForStartPositionZero()
    {
        var eventStore = CreateEventStore();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";

        var testEvent = new TestEventCreated(id, name);
        var eventId = Guid.NewGuid();

        var eventData = new EventData<TestEvent>(eventId, testEvent, DateTimeOffset.UtcNow);

        await eventStore.AppendToStream(id, 0, eventData);

        var shouldThrowAction = async () =>
        {
            await eventStore.ReadStreamForwards(id, 0, 1);
        };

        await shouldThrowAction.ShouldThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ThrowsExceptionForNumberEventsZero()
    {
        var eventStore = CreateEventStore();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";

        var testEvent = new TestEventCreated(id, name);
        var eventId = Guid.NewGuid();

        var eventData = new EventData<TestEvent>(eventId, testEvent, DateTimeOffset.UtcNow);

        await eventStore.AppendToStream(id, 0, eventData);

        var shouldThrowAction = async () =>
        {
            await eventStore.ReadStreamForwards(id, 1, 0);
        };

        await shouldThrowAction.ShouldThrowAsync<ArgumentOutOfRangeException>();
    }

    private static EventStore<TestEvent> CreateEventStore()
    {
        var container = new ContainerMock.ContainerMock();
        var feedIteratorFactory = new TestFeedIteratorFactory();
        var serializationTypeMap = new SimpleSerializationTypeMap(new[] { typeof(TestEventCreated), typeof(TestEventModified) });

        return new EventStore<TestEvent>(container, feedIteratorFactory, serializationTypeMap);
    }
}
