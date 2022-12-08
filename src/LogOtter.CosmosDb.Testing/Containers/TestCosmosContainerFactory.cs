using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Testing;

public class TestCosmosContainerFactory : ICosmosContainerFactory
{
    private readonly ConcurrentBag<ContainerMock.ContainerMock> _containers = new();

    public Task<Container> CreateContainerIfNotExistsAsync(
        string containerName,
        string partitionKeyPath,
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = null,
        IEnumerable<Collection<CompositePath>>? compositeIndexes = null,
        ThroughputProperties? throughputProperties = null
    )
    {
        var existingContainer = _containers
            .FirstOrDefault(c => string.Equals(c.Id, containerName, StringComparison.InvariantCulture));

        if (existingContainer != null)
        {
            return Task.FromResult<Container>(existingContainer);
        }

        var container = new ContainerMock.ContainerMock(partitionKeyPath, uniqueKeyPolicy, containerName, defaultTimeToLive ?? -1);
        _containers.Add(container);
        
        return Task.FromResult<Container>(container);
    }

    public Container GetContainer(string containerName)
    {
        return _containers.Single(c => c.Id == containerName);
    }
}