using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using LogOtter.CosmosDb.Metadata;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Testing;

public class TestCosmosContainerFactory : ICosmosContainerFactory
{
    private readonly ConcurrentBag<ContainerMock.ContainerMock> _containers = new();

    public TestCosmosContainerFactory() { }

    internal TestCosmosContainerFactory(
        IEnumerable<PreProvisionedContainer> preProvisionedContainers,
        IEnumerable<ContainerAutoProvisionMetadata> autoProvisionedContainers
    )
    {
        foreach (var container in preProvisionedContainers)
        {
            CreateContainerIfNotExists(container.ContainerName, container.PartitionKeyPath, container.UniqueKeyPolicy, container.DefaultTimeToLive);
        }

        foreach (var container in autoProvisionedContainers)
        {
            var metadata = container.AutoProvisionMetadata;
            CreateContainerIfNotExists(container.ContainerName, metadata.PartitionKeyPath, metadata.UniqueKeyPolicy, metadata.DefaultTimeToLive);
        }
    }

    public Task<Container> CreateContainerIfNotExistsAsync(
        string containerName,
        string partitionKeyPath,
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = null,
        IndexingPolicy? indexingPolicy = null,
        ThroughputProperties? throughputProperties = null,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult<Container>(CreateContainerIfNotExists(containerName, partitionKeyPath, uniqueKeyPolicy, defaultTimeToLive));
    }

    public Container GetContainer(string containerName)
    {
        return _containers.FirstOrDefault(c => c.Id == containerName)
            ?? throw new InvalidOperationException(
                $"Container {containerName} has not been pre-provisioned. Use WithPreProvisionedContainer<T>(\"{containerName}\") to pre-provision it"
            );
    }

    private ContainerMock.ContainerMock CreateContainerIfNotExists(
        string containerName,
        string partitionKeyPath,
        UniqueKeyPolicy? uniqueKeyPolicy,
        int? defaultTimeToLive
    )
    {
        var existingContainer = _containers.FirstOrDefault(c => string.Equals(c.Id, containerName, StringComparison.InvariantCulture));

        if (existingContainer != null)
        {
            return existingContainer;
        }

        var container = new ContainerMock.ContainerMock(partitionKeyPath, uniqueKeyPolicy, containerName, defaultTimeToLive ?? -1);
        _containers.Add(container);

        return container;
    }
}
