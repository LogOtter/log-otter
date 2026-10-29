using LogOtter.CosmosDb.EventStore.Tests.TestEvents;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class ContainerRegistrationTests
{
    [Fact]
    public void RegisteringASnapshotForAProjectionThatIsAlreadyRegisteredAsASubTypeThrows()
    {
        var builder = new ServiceCollection()
            .AddCosmosDb()
            .AddContainer<ISnapshot>("Snapshots", c => c.WithSubTypeThatMustHaveDistinctPartition<TestEventProjection>())
            .AddEventSourcing();

        var exception = Should.Throw<InvalidOperationException>(() =>
            builder.AddEventSource<TestEvent>(
                "TestEventStreams",
                c => c.AddProjection<TestEventProjection>().WithSnapshot("TestEventSnapshots", _ => TestEventProjection.StaticPartitionKey)
            )
        );

        exception.Message.ShouldBe(
            $"Container for {nameof(TestEventProjection)} cannot be registered as it is already registered as a subtype of {nameof(ISnapshot)} in container Snapshots"
        );
    }
}
