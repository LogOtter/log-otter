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
            .WithDatabase(new CosmosDbTestcontainerConfiguration())
            .Build();
    }

    public TestCosmos CreateTestCosmos()
    {
        var cosmosClient = new CosmosClient(_container.ConnectionString, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () => _container.HttpClient
        });

        return new TestCosmos(cosmosClient);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}