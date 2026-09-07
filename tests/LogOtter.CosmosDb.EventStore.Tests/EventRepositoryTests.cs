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

    [Fact]
    public async Task EnricherMetadataIsStored()
    {
        var (eventRepository, eventStore) = CreateEventStoreAndRepository(new StubMetadataEnricher(("traceparent", "trace-123")));

        var id = Guid.NewGuid().ToString();
        await eventRepository.ApplyEvents(id, null, new TestEventCreated(id, "Bob"));

        var storedEvents = await eventStore.ReadStreamForwards(id, TestContext.Current.CancellationToken);
        storedEvents.Single().Metadata.ShouldContainKeyAndValue("traceparent", "trace-123");
    }

    [Fact]
    public async Task AllEventsInBatchShareEnricherMetadata()
    {
        var (eventRepository, eventStore) = CreateEventStoreAndRepository(new StubMetadataEnricher(("traceparent", "trace-123")));

        var id = Guid.NewGuid().ToString();
        await eventRepository.ApplyEvents(id, null, new TestEventCreated(id, "Bob"), new TestEventModified(id, "Bobby"));

        var storedEvents = await eventStore.ReadStreamForwards(id, TestContext.Current.CancellationToken);
        storedEvents.Count.ShouldBe(2);
        storedEvents.ShouldAllBe(e => e.Metadata["traceparent"] == "trace-123");
    }

    [Fact]
    public async Task AdditionalMetadataIsStored()
    {
        var (eventRepository, eventStore) = CreateEventStoreAndRepository();

        var id = Guid.NewGuid().ToString();
        var additionalMetadata = new Dictionary<string, string> { ["idempotencyKey"] = "key-1" };
        await eventRepository.ApplyEvents(id, null, additionalMetadata, new TestEventCreated(id, "Bob"));

        var storedEvents = await eventStore.ReadStreamForwards(id, TestContext.Current.CancellationToken);
        storedEvents.Single().Metadata.ShouldContainKeyAndValue("idempotencyKey", "key-1");
    }

    [Fact]
    public async Task AdditionalMetadataOverridesEnricherOnKeyCollision()
    {
        var (eventRepository, eventStore) = CreateEventStoreAndRepository(new StubMetadataEnricher(("traceparent", "from-enricher")));

        var id = Guid.NewGuid().ToString();
        var additionalMetadata = new Dictionary<string, string> { ["traceparent"] = "from-caller" };
        await eventRepository.ApplyEvents(id, null, additionalMetadata, new TestEventCreated(id, "Bob"));

        var storedEvents = await eventStore.ReadStreamForwards(id, TestContext.Current.CancellationToken);
        storedEvents.Single().Metadata.ShouldContainKeyAndValue("traceparent", "from-caller");
    }

    [Fact]
    public async Task NoEnrichersResultsInEmptyMetadata()
    {
        var (eventRepository, eventStore) = CreateEventStoreAndRepository();

        var id = Guid.NewGuid().ToString();
        await eventRepository.ApplyEvents(id, null, new TestEventCreated(id, "Bob"));

        var storedEvents = await eventStore.ReadStreamForwards(id, TestContext.Current.CancellationToken);
        storedEvents.Single().Metadata.ShouldBeEmpty();
    }

    private static EventRepository<TestEvent, TestEventProjection> CreateEventRepository()
    {
        var (eventRepository, _) = CreateEventStoreAndRepository();
        return eventRepository;
    }

    private static (EventRepository<TestEvent, TestEventProjection>, EventStore<TestEvent>) CreateEventStoreAndRepository(
        params IEventMetadataEnricher[] enrichers
    )
    {
        var container = new ContainerMock.ContainerMock();
        var feedIteratorFactory = new TestFeedIteratorFactory();
        var serializationTypeMap = new SimpleSerializationTypeMap(new[] { typeof(TestEventCreated), typeof(TestEventModified) });
        var eventStore = new EventStore<TestEvent>(container, feedIteratorFactory, serializationTypeMap);
        var options = new OptionsWrapper<EventStoreOptions>(new EventStoreOptions());
        var eventRepository = new EventRepository<TestEvent, TestEventProjection>(eventStore, options, enrichers);
        return (eventRepository, eventStore);
    }

    private sealed class StubMetadataEnricher(params (string Key, string Value)[] entries) : IEventMetadataEnricher
    {
        public void Enrich(IDictionary<string, string> metadata)
        {
            foreach (var (key, value) in entries)
            {
                metadata[key] = value;
            }
        }
    }
}
