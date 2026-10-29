using LogOtter.CosmosDb.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class TestContainerSerializationTests
{
    [Fact]
    public async Task TestContainersUseTheSerializerOptionsFromTheLiveConfig()
    {
        var services = new ServiceCollection();

        services
            .AddCosmosDb(o =>
                o.ClientOptions = new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase },
                }
            )
            .WithAutoProvisioning()
            .AddContainer<Document>("Documents");

        services.AddTestCosmosDb();

        var container = services.BuildServiceProvider().GetRequiredService<CosmosContainer<Document>>().Container;

        await container.CreateItemAsync(new Document("1", "a"), new PartitionKey("a"), cancellationToken: TestContext.Current.CancellationToken);
        var response = await container.ReadItemAsync<Document>("1", new PartitionKey("a"), cancellationToken: TestContext.Current.CancellationToken);

        response.Resource.ShouldBe(new Document("1", "a"));
    }

    private record Document(string Id, string PartitionKey);
}
