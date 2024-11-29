using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
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
            // _container = new ContainerBuilder()

            //     .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()))
            //     .Build();
            _container = new CosmosDbBuilder()
                .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview")
                .WithCommand("--protocol", "https")
                .WithEnvironment("ENABLE_EXPLORER", "false")
                .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()))
                .Build();
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

    private sealed class WaitUntil : IWaitUntil
    {
        public async Task<bool> UntilAsync(IContainer container)
        {
            // CosmosDB's preconfigured HTTP client will redirect the request to the container.
            const string requestUri = "https://localhost";

            var httpClient = ((CosmosDbContainer)container).HttpClient;

            try
            {
                using var httpResponse = await httpClient.GetAsync(requestUri).ConfigureAwait(false);

                return httpResponse.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                httpClient.Dispose();
            }
        }
    }
}
