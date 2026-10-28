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
        Services.AddSingleton(new PreProvisionedContainer(containerName, typeof(TDocument), partitionKeyPath, uniqueKeyPolicy, defaultTimeToLive));

        Services.AddSingleton(sp =>
        {
            var cosmosContainerFactory = sp.GetRequiredService<ICosmosContainerFactory>();

            return new CosmosContainer<TDocument>(cosmosContainerFactory.GetContainer(containerName));
        });
        return this;
    }
}
