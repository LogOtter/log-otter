using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Metadata;

internal record AutoProvisionMetadata(
    string PartitionKeyPath = "/partitionKey",
    UniqueKeyPolicy? UniqueKeyPolicy = null,
    int? DefaultTimeToLive = -1,
    IEnumerable<Collection<CompositePath>>? CompositeIndexes = null,
    ThroughputProperties? ThroughputProperties = null
);
