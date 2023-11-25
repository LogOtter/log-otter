using LogOtter.CosmosDb.EventStore.Tests.TestEvents;
using LogOtter.CosmosDb.Testing;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class EventRepositoryTests
{
    [Fact]
    public async Task SingleEventIsStoredCorrectly()
    {
        var eventRepository = CreateEventRepository();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";

        var testEvent = new TestEventCreated(id, name);

        await eventRepository.ApplyEvents(id, null, testEvent);

        var allEvents = await eventRepository.GetEventStream(id);
        allEvents.Should().HaveCount(1);

        var createdEvent = allEvents.First();
        createdEvent.Id.Should().Be(id);
        createdEvent.EventStreamId.Should().Be(id);
    }

    [Fact]
    public async Task MultipleEventsAreStoredCorrectly()
    {
        var eventRepository = CreateEventRepository();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";
        var newName = "Bobby Bobertson";

        var testCreatedEvent = new TestEventCreated(id, name);
        var testModifiedEvent = new TestEventModified(id, newName);

        await eventRepository.ApplyEvents(id, null, testCreatedEvent, testModifiedEvent);

        var allEvents = await eventRepository.GetEventStream(id);
        allEvents.Should().HaveCount(2);

        var createdEvent = allEvents.First();
        createdEvent.Should().BeOfType<TestEventCreated>().Which.Name.Should().Be(name);

        var modifiedEvent = allEvents.Skip(1).First();
        modifiedEvent.Should().BeOfType<TestEventModified>().Which.NewName.Should().Be(newName);
    }

    [Fact]
    public async Task GetSnapshotWithZeroEvents()
    {
        var eventRepository = CreateEventRepository();

        var id = Guid.NewGuid().ToString();

        var projection = await eventRepository.Get(id);
        projection.Should().BeNull();
    }

    [Fact]
    public async Task GetSnapshotWithSingleEvents()
    {
        var eventRepository = CreateEventRepository();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";

        var testCreatedEvent = new TestEventCreated(id, name);

        await eventRepository.ApplyEvents(id, null, testCreatedEvent);

        var projection = await eventRepository.Get(id);
        projection.Should().NotBeNull();
        projection!.Id.Should().Be(id);
        projection.Name.Should().Be(name);
    }

    [Fact]
    public async Task GetSnapshotWithMultipleEvents()
    {
        var eventRepository = CreateEventRepository();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";
        var newName = "Bobby Bobertson";

        var testCreatedEvent = new TestEventCreated(id, name);
        var testModifiedEvent = new TestEventModified(id, newName);

        await eventRepository.ApplyEvents(id, null, testCreatedEvent, testModifiedEvent);

        var projection = await eventRepository.Get(id);
        projection.Should().NotBeNull();
        projection!.Id.Should().Be(id);
        projection.Name.Should().Be(newName);
    }

    private static EventRepository<TestEvent, TestEventProjection> CreateEventRepository()
    {
        var container = new ContainerMock.ContainerMock();
        var feedIteratorFactory = new TestFeedIteratorFactory();
        var serializationTypeMap = new SimpleSerializationTypeMap(new[] { typeof(TestEventCreated), typeof(TestEventModified) });
        var eventStore = new EventStore<TestEvent>(container, feedIteratorFactory, serializationTypeMap);
        var options = new OptionsWrapper<EventStoreOptions>(new EventStoreOptions());
        return new EventRepository<TestEvent, TestEventProjection>(eventStore, options);
    }
}
