using Microsoft.Azure.Cosmos;
using Testcontainers.CosmosDb;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

public class IntegrationTestsFixture : IAsyncLifetime
{
    private readonly CosmosDbContainer? _container;
    private readonly string _cosmosConnectionString;
    private readonly bool _useTestContainers;

    public IntegrationTestsFixture()
    {
        _cosmosConnectionString = Environment.GetEnvironmentVariable("TEST_COSMOS_CONNECTION_STRING") ?? "";
        _useTestContainers = string.IsNullOrWhiteSpace(_cosmosConnectionString);

        if (_useTestContainers)
        {
            _container = new CosmosDbBuilder().WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "2").Build();
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

    public TestCosmos CreateTestCosmos(CosmosClientOptions? clientOptions = null)
    {
        clientOptions ??= new CosmosClientOptions();
        if (_useTestContainers)
        {
            clientOptions.ConnectionMode = ConnectionMode.Gateway;
            clientOptions.HttpClientFactory = () => _container!.HttpClient;
            clientOptions.RequestTimeout = TimeSpan.FromMinutes(3);

            return new TestCosmos(new CosmosClient(_container!.GetConnectionString(), clientOptions), clientOptions);
        }

        return new TestCosmos(new CosmosClient(_cosmosConnectionString, clientOptions), clientOptions);
    }
}
