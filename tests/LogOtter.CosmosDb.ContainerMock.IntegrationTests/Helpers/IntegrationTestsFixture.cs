using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Azure.Cosmos;
using Xunit;

#pragma warning disable CS0618

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

public class IntegrationTestsFixture : IAsyncLifetime
{
    private readonly CosmosDbTestcontainer? _container;
    private readonly string _cosmosConnectionString;
    private readonly bool _useTestContainers;

    public IntegrationTestsFixture()
    {
        _cosmosConnectionString = Environment.GetEnvironmentVariable("TEST_COSMOS_CONNECTION_STRING") ?? "";
        _useTestContainers = string.IsNullOrWhiteSpace(_cosmosConnectionString);

        if (_useTestContainers)
        {
            _container = new ContainerBuilder<CosmosDbTestcontainer>().WithDatabase(new CosmosDbTestcontainerConfiguration()).Build();
        }
    }

    public async Task InitializeAsync()
    {
        if (_useTestContainers)
        {
            await _container!.StartAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_useTestContainers)
        {
            await _container!.DisposeAsync();
        }
    }

    public TestCosmos CreateTestCosmos()
    {
        var cosmosClient = !_useTestContainers
            ? new CosmosClient(_cosmosConnectionString)
            : new CosmosClient(
                _container!.ConnectionString,
                new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    HttpClientFactory = () => _container.HttpClient,
                    RequestTimeout = TimeSpan.FromMinutes(3)
                }
            );

        return new TestCosmos(cosmosClient);
    }
}
