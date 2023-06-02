using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.Testing;

public interface ITestCosmosDbBuilder
{
    public IServiceCollection Services { get; }

    public ITestCosmosDbBuilder WithPreProvisionedContainer<TDocument>(
        string containerName,
        string partitionKeyPath = "/partitionKey",
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = -1,
        IndexingPolicy? indexingPolicy = null,
        ThroughputProperties? throughputProperties = null
    );
}
