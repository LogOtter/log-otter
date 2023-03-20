using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

public class CosmosContainerFactory : ICosmosContainerFactory
{
    private readonly Database _database;

    public CosmosContainerFactory(Database database)
    {
        _database = database;
    }

    public async Task<Container> CreateContainerIfNotExistsAsync(
        string containerName,
        string partitionKeyPath,
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = null,
        IEnumerable<Collection<CompositePath>>? compositeIndexes = null,
        ThroughputProperties? throughputProperties = null
    )
    {
        var containerProperties = new ContainerProperties(containerName, partitionKeyPath) { DefaultTimeToLive = defaultTimeToLive };

        if (uniqueKeyPolicy != null)
        {
            containerProperties.UniqueKeyPolicy = uniqueKeyPolicy;
        }

        if (compositeIndexes != null)
        {
            foreach (var index in compositeIndexes)
            {
                containerProperties.IndexingPolicy.CompositeIndexes.Add(index);
            }
        }

        var containerResponse = await _database.CreateContainerIfNotExistsAsync(containerProperties, throughputProperties);

        return containerResponse.Container;
    }

    public Container GetContainer(string containerName)
    {
        return _database.GetContainer(containerName);
    }
}
