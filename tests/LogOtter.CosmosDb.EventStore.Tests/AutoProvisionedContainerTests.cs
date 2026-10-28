using LogOtter.CosmosDb.EventStore.Tests.TestEvents;
using LogOtter.CosmosDb.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class AutoProvisionedContainerTests
{
    private const string EventContainerName = "TestEventStreams";
    private const string SnapshotContainerName = "TestEventSnapshots";
    private const string DocumentContainerName = "Documents";

    [Fact]
    public async Task AutoProvisionedContainersDoNotNeedToBePreProvisioned()
    {
        using var host = await StartAutoProvisionedHostWithDefaultContainersAndEventSource(_ => { });

        var eventRepository = host.Services.GetRequiredService<EventRepository<TestEvent, TestEventProjection>>();
        var containerFactory = host.Services.GetRequiredService<TestCosmosContainerFactory>();

        var id = Guid.NewGuid().ToString();
        await eventRepository.ApplyEvents(id, null, new TestEventCreated(id, "Bob Bobertson"));

        var eventContainer = (ContainerMock.ContainerMock)containerFactory.GetContainer(EventContainerName);
        var snapshotContainer = (ContainerMock.ContainerMock)containerFactory.GetContainer(SnapshotContainerName);

        eventContainer.GetAllItems<CosmosDbStorageEvent>().ShouldHaveSingleItem().Document.StreamId.ShouldBe(id);
        snapshotContainer.GetAllItems<TestEventProjection>().ShouldHaveSingleItem().Document.Id.ShouldBe(id);
    }

    [Fact]
    public async Task AutoProvisionedContainersUseTheirRegisteredAutoProvisionSettings()
    {
        using var host = await StartAutoProvisionedHostWithDefaultContainersAndEventSource(_ => { });

        var container = host.Services.GetRequiredService<CosmosContainer<Document>>().Container;
        await container.UpsertItemAsync(new Document { Id = "1", CustomKey = "a" }, cancellationToken: TestContext.Current.CancellationToken);

        await container.ReadItemAsync<Document>("1", new PartitionKey("a"), cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PreProvisioningAnAutoProvisionedContainerThrows()
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            StartAutoProvisionedHostWithDefaultContainersAndEventSource(builder =>
                builder.WithPreProvisionedContainer<Document>(DocumentContainerName)
            )
        );

        exception.Message.ShouldContain(
            $"Container {DocumentContainerName} is pre-provisioned in the test setup, but the live config uses WithAutoProvisioning() so it will be created automatically. Either remove the pre-provisioning from the test setup, or remove WithAutoProvisioning() from the live config if the container should already exist"
        );
    }

    [Fact]
    public async Task PreProvisioningAnUnregisteredTypeInAnUnregisteredContainerIsAllowed()
    {
        using var host = await StartAutoProvisionedHostWithDefaultContainersAndEventSource(builder =>
            builder.WithPreProvisionedContainer<UnregisteredDocument>("TestOnlyContainer")
        );

        var container = host.Services.GetRequiredService<CosmosContainer<UnregisteredDocument>>();

        container.Container.Id.ShouldBe("TestOnlyContainer");
    }

    private static async Task<IHost> StartAutoProvisionedHostWithDefaultContainersAndEventSource(Action<ITestCosmosDbBuilder> configureTestCosmosDb)
    {
        var builder = Host.CreateApplicationBuilder();

        builder
            .Services.AddCosmosDb()
            .WithAutoProvisioning()
            .AddContainer<Document>(DocumentContainerName, c => c.WithAutoProvisionSettings(partitionKeyPath: "/customKey"))
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

    private record Document
    {
        [JsonProperty("id")]
        public string Id { get; init; } = "";

        [JsonProperty("customKey")]
        public string CustomKey { get; init; } = "";
    }

    private record UnregisteredDocument;
}
