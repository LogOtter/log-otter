using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.Testing;

public class TestCosmosDbBuilder(IServiceCollection services) : ITestCosmosDbBuilder
{
    public IServiceCollection Services { get; } = services;

    public ITestCosmosDbBuilder WithPreProvisionedContainer<TDocument>(
        string containerName,
        string partitionKeyPath = "/partitionKey",
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = -1,
        IndexingPolicy? indexingPolicy = null,
        ThroughputProperties? throughputProperties = null
    )
    {
        Services.AddSingleton(sp =>
        {
            var cosmosContainerFactory = sp.GetRequiredService<ICosmosContainerFactory>();

            var container = cosmosContainerFactory
                .CreateContainerIfNotExistsAsync(
                    containerName,
                    partitionKeyPath,
                    uniqueKeyPolicy,
                    defaultTimeToLive,
                    indexingPolicy,
                    throughputProperties
                )
                .GetAwaiter()
                .GetResult();

            return new CosmosContainer<TDocument>(container);
        });
        return this;
    }
}
