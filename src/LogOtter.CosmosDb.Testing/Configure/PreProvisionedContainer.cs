using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Testing;

internal record PreProvisionedContainer(
    string ContainerName,
    Type DocumentType,
    string PartitionKeyPath,
    UniqueKeyPolicy? UniqueKeyPolicy,
    int? DefaultTimeToLive
);
