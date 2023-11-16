using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

public class CosmosContainerFactory(Database database) : ICosmosContainerFactory
{
    public async Task<Container> CreateContainerIfNotExistsAsync(
        string containerName,
        string partitionKeyPath,
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = null,
        IndexingPolicy? indexingPolicy = null,
        ThroughputProperties? throughputProperties = null
    )
    {
        var containerProperties = new ContainerProperties(containerName, partitionKeyPath) { DefaultTimeToLive = defaultTimeToLive };

        if (uniqueKeyPolicy != null)
        {
            containerProperties.UniqueKeyPolicy = uniqueKeyPolicy;
        }

        if (indexingPolicy != null)
        {
            containerProperties.IndexingPolicy = indexingPolicy;
        }

        var containerResponse = await database.CreateContainerIfNotExistsAsync(containerProperties, throughputProperties);

        return containerResponse.Container;
    }

    public Container GetContainer(string containerName)
    {
        return database.GetContainer(containerName);
    }
}
