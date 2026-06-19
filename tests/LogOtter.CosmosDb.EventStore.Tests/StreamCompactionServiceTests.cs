using LogOtter.CosmosDb.EventStore.Tests.TestEvents;
using LogOtter.CosmosDb.Testing;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class StreamCompactionServiceTests
{
    [Fact]
    public async Task CompactSingleEventStreamReplacesEventWithTombstone()
    {
        var (compactionService, eventStore, _, _) = CreateServices();

        var id = Guid.NewGuid().ToString();
        await eventStore.AppendToStream(id, 0, new EventData<TestEvent>(Guid.NewGuid(), new TestEventCreated(id, "Alice"), DateTimeOffset.UtcNow));

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        var events = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        events.Count.ShouldBe(1);
        events.First().EventBody.ShouldBeOfType<TestEventCompacted>().ScrubbedName.ShouldBe("[REDACTED]");
        events.First().EventNumber.ShouldBe(1);
    }

    [Fact]
    public async Task CompactMultiEventStreamDeletesOriginalEvents()
    {
        var (compactionService, eventStore, _, _) = CreateServices();

        var id = Guid.NewGuid().ToString();
        await eventStore.AppendToStream(
            id,
            0,
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventCreated(id, "Alice"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Alice Smith"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Alice Jones"), DateTimeOffset.UtcNow)
        );

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        var events = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        events.Count.ShouldBe(1);
        events.First().EventNumber.ShouldBe(1);
        events.First().EventBody.ShouldBeOfType<TestEventCompacted>();
    }

    [Fact]
    public async Task AppendAfterCompactionIsIgnoredByProjection()
    {
        // Stream-level append-after-compaction still works (the raw store doesn't know about
        // tombstones), but the read path treats anything after the tombstone as a leftover and
        // drops it from the projection. This is the cost of the safe-retry design: once a stream
        // is compacted, its state is frozen at the tombstone.
        var (compactionService, eventStore, eventRepository, _) = CreateServices();

        var id = Guid.NewGuid().ToString();
        await eventStore.AppendToStream(
            id,
            0,
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventCreated(id, "Alice"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Alice Smith"), DateTimeOffset.UtcNow)
        );

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        await eventStore.AppendToStream(id, 1, new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "New Name"), DateTimeOffset.UtcNow));

        var events = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        events.Count.ShouldBe(2, "the raw store accepts the append");

        var projection = await eventRepository.Get(id, cancellationToken: TestContext.Current.CancellationToken);
        projection.ShouldNotBeNull();
        projection.Name.ShouldBe("[REDACTED]", "the projection stops at the tombstone and ignores subsequent events");
    }

    [Fact]
    public async Task CompactingEmptyStreamIsNoOp()
    {
        var (compactionService, eventStore, _, _) = CreateServices();

        var id = Guid.NewGuid().ToString();

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        var events = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        events.Count.ShouldBe(0);
    }

    [Fact]
    public async Task CompactLargeStreamWithMoreThan100EventsWorksAcrossBatches()
    {
        var (compactionService, eventStore, _, _) = CreateServices();

        var id = Guid.NewGuid().ToString();
        var initial = new EventData<TestEvent>(Guid.NewGuid(), new TestEventCreated(id, "Alice"), DateTimeOffset.UtcNow);
        await eventStore.AppendToStream(id, 0, initial);

        // append 250 more events one at a time so each goes in its own append (avoids large transactional batch on insert)
        for (var i = 0; i < 250; i++)
        {
            var modified = new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, $"Name {i}"), DateTimeOffset.UtcNow);
            await eventStore.AppendToStream(id, i + 1, modified);
        }

        var totalBefore = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        totalBefore.Count.ShouldBe(251);

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        var events = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        events.Count.ShouldBe(1);
        events.First().EventNumber.ShouldBe(1);
        events.First().EventBody.ShouldBeOfType<TestEventCompacted>();
    }

    [Fact]
    public async Task RecompactingAnAlreadyCompactedStreamIsSafe()
    {
        var (compactionService, eventStore, _, _) = CreateServices();

        var id = Guid.NewGuid().ToString();
        await eventStore.AppendToStream(
            id,
            0,
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventCreated(id, "Alice"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Bob"), DateTimeOffset.UtcNow)
        );

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);
        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        var events = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        events.Count.ShouldBe(1);
        events.First().EventNumber.ShouldBe(1);
        events.First().EventBody.ShouldBeOfType<TestEventCompacted>();
    }

    [Fact]
    public async Task CompactionResetsRepositoryRevisionToOne()
    {
        var (compactionService, _, eventRepository, _) = CreateServices();

        var id = Guid.NewGuid().ToString();
        await eventRepository.ApplyEvents(
            id,
            null,
            new TestEventCreated(id, "Alice"),
            new TestEventModified(id, "Bob"),
            new TestEventModified(id, "Charlie")
        );

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        var snapshot = await eventRepository.Get(id, cancellationToken: TestContext.Current.CancellationToken);
        snapshot.ShouldNotBeNull();
        snapshot.Revision.ShouldBe(1);
        snapshot.Name.ShouldBe("[REDACTED]");
    }

    [Fact]
    public async Task AppendWithStaleRevisionAfterCompactionThrowsConcurrencyException()
    {
        var (compactionService, eventStore, _, _) = CreateServices();

        var id = Guid.NewGuid().ToString();
        await eventStore.AppendToStream(
            id,
            0,
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventCreated(id, "Alice"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Bob"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Charlie"), DateTimeOffset.UtcNow)
        );

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        var staleAppend = async () =>
            await eventStore.AppendToStream(
                id,
                3,
                new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Dave"), DateTimeOffset.UtcNow)
            );

        await staleAppend.ShouldThrowAsync<ConcurrencyException>();
    }

    [Fact]
    public async Task PartiallyCompactedStream_Retry_ProducesTombstoneStateRegardlessOfLeftovers()
    {
        // Simulates a crash mid-compaction: the tombstone is in place at event 1, but tail cleanup
        // didn't finish so events 2..K still linger. With the old design, retrying would re-derive
        // the projection from the truncated stream and produce a stale tombstone. With the new
        // design (tombstone-first + ICompactionEvent marker), the retry reuses the existing
        // tombstone — the original state is preserved.
        var (compactionService, eventStore, eventRepository, _) = CreateServices();

        var id = Guid.NewGuid().ToString();
        await eventStore.AppendToStream(
            id,
            0,
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventCreated(id, "Alice"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Bob"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Charlie"), DateTimeOffset.UtcNow)
        );

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        // Simulate a stale leftover from a crashed cleanup: append an event after the tombstone.
        // (In real life this would be a leftover event that the cleanup hadn't deleted yet, or a
        // concurrent write that snuck in. AppendToStream still works because event 1 exists.)
        await eventStore.AppendToStream(
            id,
            1,
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Stale Leftover"), DateTimeOffset.UtcNow)
        );

        // Projection must still reflect the tombstone — leftovers do not corrupt the read.
        var snapshotBeforeRetry = await eventRepository.Get(id, cancellationToken: TestContext.Current.CancellationToken);
        snapshotBeforeRetry.ShouldNotBeNull();
        snapshotBeforeRetry.Name.ShouldBe("[REDACTED]");
        snapshotBeforeRetry.Revision.ShouldBe(1);

        // Retry compaction — should clean up the leftover and keep the tombstone state.
        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        var events = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        events.Count.ShouldBe(1);
        events.First().EventBody.ShouldBeOfType<TestEventCompacted>().ScrubbedName.ShouldBe("[REDACTED]");

        var snapshotAfterRetry = await eventRepository.Get(id, cancellationToken: TestContext.Current.CancellationToken);
        snapshotAfterRetry.ShouldNotBeNull();
        snapshotAfterRetry.Name.ShouldBe("[REDACTED]");
    }

    [Fact]
    public async Task RetryDoesNotRederiveProjectionFromTruncatedStream()
    {
        // The core invariant from the proposal review: after a partial compaction, the retry must
        // not pass leftover events back through the compactor and produce a stale tombstone.
        var (compactionService, eventStore, eventRepository, _) = CreateServices();

        var id = Guid.NewGuid().ToString();

        // Pre-set: tombstone scrubs "Charlie" (the original final state)
        await eventStore.AppendToStream(
            id,
            0,
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventCreated(id, "Alice"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Bob"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Charlie"), DateTimeOffset.UtcNow)
        );
        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        // Inject a misleading leftover — its NewName is different from the original final state.
        // If the retry re-derived the projection it would see [tombstone, "Wrong"] and the
        // compactor would scrub "Wrong" into the tombstone. The tombstone EventId would change.
        var originalTombstone = (await eventStore.ReadStreamForwards(id, TestContext.Current.CancellationToken)).Single();
        await eventStore.AppendToStream(
            id,
            1,
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Wrong State"), DateTimeOffset.UtcNow)
        );

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        var events = await eventStore.ReadStreamForwards(id, cancellationToken: TestContext.Current.CancellationToken);
        events.Count.ShouldBe(1);
        events.First().EventId.ShouldBe(originalTombstone.EventId, "the retry should reuse the original tombstone rather than synthesise a new one from the truncated stream");
    }

    [Fact]
    public async Task CompactionUpsertsSnapshotAtRevisionOne()
    {
        var (compactionService, eventStore, _, snapshotRepository) = CreateServices();

        var id = Guid.NewGuid().ToString();
        await eventStore.AppendToStream(
            id,
            0,
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventCreated(id, "Alice"), DateTimeOffset.UtcNow),
            new EventData<TestEvent>(Guid.NewGuid(), new TestEventModified(id, "Bob"), DateTimeOffset.UtcNow)
        );

        await compactionService.CompactStream(id, TestContext.Current.CancellationToken);

        var snapshot = await snapshotRepository.GetSnapshot(
            id,
            TestEventProjection.StaticPartitionKey,
            cancellationToken: TestContext.Current.CancellationToken
        );
        snapshot.ShouldNotBeNull();
        snapshot.Revision.ShouldBe(1);
        snapshot.Name.ShouldBe("[REDACTED]");
    }

    private static (
        StreamCompactionService<TestEvent, TestEventProjection> compactionService,
        EventStore<TestEvent> eventStore,
        EventRepository<TestEvent, TestEventProjection> eventRepository,
        SnapshotRepository<TestEvent, TestEventProjection> snapshotRepository
    ) CreateServices()
    {
        var container = new ContainerMock.ContainerMock();
        var snapshotContainer = new ContainerMock.ContainerMock();
        var feedIteratorFactory = new TestFeedIteratorFactory();
        var serializationTypeMap = new SimpleSerializationTypeMap(
            new[] { typeof(TestEventCreated), typeof(TestEventModified), typeof(TestEventCompacted) }
        );
        var eventStore = new EventStore<TestEvent>(container, feedIteratorFactory, serializationTypeMap);
        var options = new OptionsWrapper<EventStoreOptions>(new EventStoreOptions());
        var eventRepository = new EventRepository<TestEvent, TestEventProjection>(eventStore, options);
        var snapshotRepository = new SnapshotRepository<TestEvent, TestEventProjection>(
            new CosmosContainer<TestEventProjection>(snapshotContainer),
            eventStore,
            feedIteratorFactory,
            options
        );
        var compactor = new TestStreamCompactor();
        var compactionService = new StreamCompactionService<TestEvent, TestEventProjection>(eventStore, snapshotRepository, compactor, options);
        return (compactionService, eventStore, eventRepository, snapshotRepository);
    }
}
