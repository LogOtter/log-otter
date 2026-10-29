using LogOtter.CosmosDb.EventStore.Tests.TestEvents;
using LogOtter.CosmosDb.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class HybridRepositoryTests
{
    [Fact]
    public async Task ApplyingEmptyEventListAndUpdatingTheSnapshotImmediatelyThrows()
    {
        var hybridRepository = CreateHybridRepository();

        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            hybridRepository.ApplyEventsAndUpdateSnapshotImmediately("id", null, TestContext.Current.CancellationToken, new List<TestEvent>())
        );

        exception.Message.ShouldBe(
            "At least one event must be provided when applying events, make sure you pass at least one event, or an IEnumerable or array with at least 1 event in it. (Parameter 'events')"
        );
    }

    [Fact]
    public async Task ApplyingEmptyEventArrayAndUpdatingTheSnapshotImmediatelyThrows()
    {
        var hybridRepository = CreateHybridRepository();

        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            hybridRepository.ApplyEventsAndUpdateSnapshotImmediately("id", null, TestContext.Current.CancellationToken, Array.Empty<TestEvent>())
        );

        exception.Message.ShouldBe(
            "At least one event must be provided when applying events, make sure you pass at least one event, or an IEnumerable or array with at least 1 event in it. (Parameter 'events')"
        );
    }

    private static HybridRepository<TestEvent, TestEventProjection> CreateHybridRepository()
    {
        var services = new ServiceCollection();

        services
            .AddCosmosDb()
            .WithAutoProvisioning()
            .AddEventSourcing()
            .AddEventSource<TestEvent>(
                "TestEventStreams",
                c => c.AddProjection<TestEventProjection>().WithSnapshot("TestEventSnapshots", _ => TestEventProjection.StaticPartitionKey)
            );

        services.AddTestCosmosDb();

        return services.BuildServiceProvider().GetRequiredService<HybridRepository<TestEvent, TestEventProjection>>();
    }
}
