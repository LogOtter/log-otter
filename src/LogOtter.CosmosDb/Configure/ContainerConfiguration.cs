using System.Collections.ObjectModel;
using LogOtter.CosmosDb.Metadata;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

public class ContainerConfiguration
{
    internal AutoProvisionMetadata? AutoProvisionMetadata { get; private set; }

    internal ContainerConfiguration() { }

    public ContainerConfiguration WithAutoProvisionSettings(
        string partitionKeyPath = "/partitionKey",
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = -1,
        IReadOnlyCollection<Collection<CompositePath>>? compositeIndexes = null,
        ThroughputProperties? throughputProperties = null
    )
    {
        AutoProvisionMetadata = new AutoProvisionMetadata(
            partitionKeyPath,
            uniqueKeyPolicy,
            defaultTimeToLive,
            compositeIndexes,
            throughputProperties
        );

        return this;
    }
}
