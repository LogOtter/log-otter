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

        var allEvents = await eventRepository.GetEventStream(id, cancellationToken: TestContext.Current.CancellationToken);
        allEvents.Count.ShouldBe(1);

        var createdEvent = allEvents.First();
        createdEvent.Id.ShouldBe(id);
        createdEvent.EventStreamId.ShouldBe(id);
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

        var allEvents = await eventRepository.GetEventStream(id, cancellationToken: TestContext.Current.CancellationToken);
        allEvents.Count.ShouldBe(2);

        var createdEvent = allEvents.First();
        createdEvent.ShouldBeOfType<TestEventCreated>().Name.ShouldBe(name);

        var modifiedEvent = allEvents.Skip(1).First();
        modifiedEvent.ShouldBeOfType<TestEventModified>().NewName.ShouldBe(newName);
    }

    [Fact]
    public async Task GetProjectionWithSingleEventOnApply()
    {
        var eventRepository = CreateEventRepository();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";

        var testCreatedEvent = new TestEventCreated(id, name);

        var projection = await eventRepository.ApplyEvents(id, null, testCreatedEvent);

        projection.ShouldNotBeNull();
        projection.Id.ShouldBe(id);
        projection.Name.ShouldBe(name);
    }

    [Fact]
    public async Task GetProjectionWithMultipleEventsOnApply()
    {
        var eventRepository = CreateEventRepository();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";
        var newName = "Bobby Bobertson";

        var testCreatedEvent = new TestEventCreated(id, name);
        var testModifiedEvent = new TestEventModified(id, newName);

        var projection = await eventRepository.ApplyEvents(id, null, testCreatedEvent, testModifiedEvent);

        projection.ShouldNotBeNull();
        projection.Id.ShouldBe(id);
        projection.Name.ShouldBe(newName);
    }

    [Fact]
    public async Task GetProjectionWithZeroEvents()
    {
        var eventRepository = CreateEventRepository();

        var id = Guid.NewGuid().ToString();

        var projection = await eventRepository.Get(id, cancellationToken: TestContext.Current.CancellationToken);
        projection.ShouldBeNull();
    }

    [Fact]
    public async Task GetProjectionWithSingleEvents()
    {
        var eventRepository = CreateEventRepository();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";

        var testCreatedEvent = new TestEventCreated(id, name);

        await eventRepository.ApplyEvents(id, null, testCreatedEvent);

        var projection = await eventRepository.Get(id, cancellationToken: TestContext.Current.CancellationToken);
        projection.ShouldNotBeNull();
        projection.Id.ShouldBe(id);
        projection.Name.ShouldBe(name);
    }

    [Fact]
    public async Task GetProjectionWithMultipleEvents()
    {
        var eventRepository = CreateEventRepository();

        var id = Guid.NewGuid().ToString();
        var name = "Bob Bobertson";
        var newName = "Bobby Bobertson";

        var testCreatedEvent = new TestEventCreated(id, name);
        var testModifiedEvent = new TestEventModified(id, newName);

        await eventRepository.ApplyEvents(id, null, testCreatedEvent, testModifiedEvent);

        var projection = await eventRepository.Get(id, cancellationToken: TestContext.Current.CancellationToken);
        projection.ShouldNotBeNull();
        projection.Id.ShouldBe(id);
        projection.Name.ShouldBe(newName);
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
