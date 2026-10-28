using LogOtter.CosmosDb.EventStore.Tests.TestEvents;
using LogOtter.CosmosDb.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class PreProvisionedContainerTests
{
    private const string EventContainerName = "TestEventStreams";
    private const string SnapshotContainerName = "TestEventSnapshots";
    private const string DocumentContainerName = "Documents";

    [Fact]
    public async Task PreProvisionedContainersMatchingTheRegisteredContainersAreUsed()
    {
        using var host = await StartHostWithDefaultContainersAndEventSource(builder => WithPreProvisionedDefaultContainersAndEventSource(builder));

        var eventRepository = host.Services.GetRequiredService<EventRepository<TestEvent, TestEventProjection>>();
        var containerFactory = host.Services.GetRequiredService<TestCosmosContainerFactory>();

        var id = Guid.NewGuid().ToString();
        await eventRepository.ApplyEvents(id, null, new TestEventCreated(id, "Bob Bobertson"));

        var eventContainer = (ContainerMock.ContainerMock)containerFactory.GetContainer(EventContainerName);
        var snapshotContainer = (ContainerMock.ContainerMock)containerFactory.GetContainer(SnapshotContainerName);

        var storedEvent = eventContainer.GetAllItems<CosmosDbStorageEvent>().ShouldHaveSingleItem().Document;
        storedEvent.StreamId.ShouldBe(id);
        storedEvent.BodyType.ShouldBe(nameof(TestEventCreated));

        var snapshot = snapshotContainer.GetAllItems<TestEventProjection>().ShouldHaveSingleItem().Document;
        snapshot.Id.ShouldBe(id);
        snapshot.Name.ShouldBe("Bob Bobertson");
    }

    [Fact]
    public async Task PreProvisioningARegisteredTypeInADifferentContainerThrows()
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            StartHostWithDefaultContainersAndEventSource(builder =>
                builder.WithPreProvisionedContainer<TestEvent>(EventContainerName).WithPreProvisionedContainer<TestEvent>(SnapshotContainerName)
            )
        );

        exception.Message.ShouldContain(
            $"{nameof(TestEvent)} is pre-provisioned in container {SnapshotContainerName} in the test setup, but the live config registers it with container {EventContainerName} (and registers container {SnapshotContainerName} for {nameof(TestEventProjection)})"
        );
    }

    [Fact]
    public async Task PreProvisioningARegisteredContainerForADifferentTypeThrows()
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            StartHostWithDefaultContainersAndEventSource(builder => builder.WithPreProvisionedContainer<UnregisteredDocument>(SnapshotContainerName))
        );

        exception.Message.ShouldContain(
            $"Container {SnapshotContainerName} is pre-provisioned for {nameof(UnregisteredDocument)} in the test setup, but the live config registers it for {nameof(TestEventProjection)}"
        );
    }

    [Fact]
    public async Task NotPreProvisioningARegisteredContainerThrows()
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => StartHostWithDefaultContainersAndEventSource(_ => { }));

        exception.Message.ShouldContain(
            $"Container {DocumentContainerName} is not pre-provisioned in the test setup, but the live config does not use WithAutoProvisioning() so the container must already exist. Either pre-provision it in the test setup using WithPreProvisionedContainer<{nameof(BaseDocument)}>(\"{DocumentContainerName}\") with the same settings as the real container, or add WithAutoProvisioning() to the live config to create it automatically"
        );
    }

    [Fact]
    public async Task PreProvisioningARegisteredSubTypeInItsBaseTypesContainerIsAllowed()
    {
        using var host = await StartHostWithDefaultContainersAndEventSource(builder =>
            WithPreProvisionedDefaultContainersAndEventSource(builder).WithPreProvisionedContainer<SubDocument>(DocumentContainerName)
        );

        var container = host.Services.GetRequiredService<CosmosContainer<SubDocument>>();

        container.Container.Id.ShouldBe(DocumentContainerName);
    }

    [Fact]
    public async Task PreProvisioningARegisteredSubTypeInADifferentContainerThrows()
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            StartHostWithDefaultContainersAndEventSource(builder => builder.WithPreProvisionedContainer<SubDocument>("OtherDocuments"))
        );

        exception.Message.ShouldContain(
            $"{nameof(SubDocument)} is pre-provisioned in container OtherDocuments in the test setup, but the live config registers it with container {DocumentContainerName}"
        );
    }

    [Fact]
    public async Task PreProvisioningAnUnregisteredSubTypeInItsBaseTypesContainerThrows()
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            StartHostWithDefaultContainersAndEventSource(builder =>
                builder.WithPreProvisionedContainer<UnregisteredSubDocument>(DocumentContainerName)
            )
        );

        exception.Message.ShouldContain(
            $"{nameof(UnregisteredSubDocument)} is pre-provisioned in container {DocumentContainerName} in the test setup, but the live config does not register it as a subtype of {nameof(BaseDocument)} (use WithSubTypeThatMustHaveDistinctPartition<{nameof(UnregisteredSubDocument)}>() to register it)"
        );
    }

    [Fact]
    public async Task PreProvisioningAnUnregisteredTypeInAnUnregisteredContainerIsAllowed()
    {
        using var host = await StartHostWithDefaultContainersAndEventSource(builder =>
            WithPreProvisionedDefaultContainersAndEventSource(builder).WithPreProvisionedContainer<UnregisteredDocument>("TestOnlyContainer")
        );

        var container = host.Services.GetRequiredService<CosmosContainer<UnregisteredDocument>>();

        container.Container.Id.ShouldBe("TestOnlyContainer");
    }

    private static ITestCosmosDbBuilder WithPreProvisionedDefaultContainersAndEventSource(ITestCosmosDbBuilder builder)
    {
        return builder
            .WithPreProvisionedContainer<BaseDocument>(DocumentContainerName)
            .WithPreProvisionedContainer<TestEvent>(EventContainerName)
            .WithPreProvisionedContainer<TestEventProjection>(SnapshotContainerName);
    }

    private static async Task<IHost> StartHostWithDefaultContainersAndEventSource(Action<ITestCosmosDbBuilder> configureTestCosmosDb)
    {
        var builder = Host.CreateApplicationBuilder();

        builder
            .Services.AddCosmosDb()
            .AddContainer<BaseDocument>(DocumentContainerName, c => c.WithSubTypeThatMustHaveDistinctPartition<SubDocument>())
            .AddEventSourcing()
            .AddEventSource<TestEvent>(
                EventContainerName,
                c => c.AddProjection<TestEventProjection>().WithSnapshot(SnapshotContainerName, _ => nameof(TestEventProjection))
            );

        configureTestCosmosDb(builder.Services.AddTestCosmosDb());

        var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        return host;
    }

    private record BaseDocument;

    private record SubDocument : BaseDocument;

    private record UnregisteredSubDocument : BaseDocument;

    private record UnregisteredDocument;
}
