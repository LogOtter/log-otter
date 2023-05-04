using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Metadata;

internal record AutoProvisionMetadata(
    string PartitionKeyPath = "/partitionKey",
    UniqueKeyPolicy? UniqueKeyPolicy = null,
    int? DefaultTimeToLive = -1,
    IndexingPolicy? IndexingPolicy = null,
    ThroughputProperties? ThroughputProperties = null
);
