using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

public class IntegrationTestsFixture : IAsyncLifetime
{
    private readonly CosmosDbTestcontainer _container;

    public IntegrationTestsFixture()
    {
        _container = new TestcontainersBuilder<CosmosDbTestcontainer>()
            .WithDatabase(new CosmosDbTestcontainerConfiguration { PartitionCount = 20 })
            .Build();
    }

    public TestCosmos CreateTestCosmos()
    {
        var cosmosClient = new CosmosClient(_container.ConnectionString, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () => _container.HttpClient, 
            RequestTimeout = TimeSpan.FromMinutes(3) 
        });

        return new TestCosmos(cosmosClient);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
        {
            // Adding small delay when running on GitHub actions since it takes a while for the ports to get mapped
            await Task.Delay(100);
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}