using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;

namespace LogOtter.EventStore.CosmosDb;

public interface ICosmosContainerFactory
{
    Container CreateContainerIfNotExistsAsync(
        string containerName,
        string partitionKeyPath,
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = null,
        IEnumerable<Collection<CompositePath>>? compositeIndexes = null,
        ThroughputProperties? throughputProperties = null
    );
}