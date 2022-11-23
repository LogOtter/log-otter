using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

public class IntegrationTestsFixture : IAsyncLifetime
{
    private readonly bool _useTestContainers;
    private readonly CosmosDbTestcontainer _container;
    private readonly string _cosmosConnectionString;

    public IntegrationTestsFixture()
    {
        _cosmosConnectionString = Environment.GetEnvironmentVariable("TEST_COSMOS_CONNECTION_STRING") ?? "";
        _useTestContainers = string.IsNullOrWhiteSpace(_cosmosConnectionString);

        _container = new TestcontainersBuilder<CosmosDbTestcontainer>()
            .WithDatabase(new CosmosDbTestcontainerConfiguration())
            .Build();
    }

    public TestCosmos CreateTestCosmos()
    {
        var cosmosClient = !_useTestContainers
            ? new CosmosClient(_cosmosConnectionString)
            : new CosmosClient(_container.ConnectionString, new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                HttpClientFactory = () => _container.HttpClient,
                RequestTimeout = TimeSpan.FromMinutes(3)
            });

        return new TestCosmos(cosmosClient);
    }

    public async Task InitializeAsync()
    {
        if (_useTestContainers)
        {
            await _container.StartAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}