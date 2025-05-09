using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

public interface ICosmosContainerFactory
{
    Task<Container> CreateContainerIfNotExistsAsync(
        string containerName,
        string partitionKeyPath,
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = null,
        IndexingPolicy? indexingPolicy = null,
        ThroughputProperties? throughputProperties = null,
        CancellationToken cancellationToken = default
    );

    Container GetContainer(string containerName);
}
