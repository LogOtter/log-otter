using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using LogOtter.CosmosDb.Metadata;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Testing;

public class TestCosmosContainerFactory : ICosmosContainerFactory
{
    private const string DefaultPartitionKeyPath = "/partitionKey";

    private readonly ConcurrentBag<ContainerMock.ContainerMock> _containers = new();
    private readonly CosmosSerializationOptions? _serializationOptions;
    private readonly ConnectionMode _connectionMode = ConnectionMode.Direct;

    public TestCosmosContainerFactory() { }

    internal TestCosmosContainerFactory(
        IEnumerable<PreProvisionedContainer> preProvisionedContainers,
        IReadOnlyCollection<ContainerAutoProvisionMetadata> registeredContainers,
        bool autoProvisioningEnabled,
        CosmosSerializationOptions? serializationOptions,
        ConnectionMode connectionMode
    )
    {
        _serializationOptions = serializationOptions;
        _connectionMode = connectionMode;

        foreach (var container in preProvisionedContainers)
        {
            var requiredPartitionKeyPath = registeredContainers
                .FirstOrDefault(c => c.ContainerName == container.ContainerName)
                ?.RequiredPartitionKeyPath;
            var partitionKeyPath = container.PartitionKeyPath ?? requiredPartitionKeyPath ?? DefaultPartitionKeyPath;

            CreateContainerIfNotExists(container.ContainerName, partitionKeyPath, container.UniqueKeyPolicy, container.DefaultTimeToLive);
        }

        if (autoProvisioningEnabled)
        {
            foreach (var container in registeredContainers)
            {
                var metadata = container.AutoProvisionMetadata;
                CreateContainerIfNotExists(container.ContainerName, metadata.PartitionKeyPath, metadata.UniqueKeyPolicy, metadata.DefaultTimeToLive);
            }
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

        var container = new ContainerMock.ContainerMock(
            partitionKeyPath,
            uniqueKeyPolicy,
            containerName,
            defaultTimeToLive ?? -1,
            _serializationOptions,
            _connectionMode
        );
        _containers.Add(container);

        return container;
    }
}
