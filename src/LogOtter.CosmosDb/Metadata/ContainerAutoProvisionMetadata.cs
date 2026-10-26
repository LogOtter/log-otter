namespace LogOtter.CosmosDb.Metadata;

internal record ContainerAutoProvisionMetadata(
    string ContainerName,
    Type DocumentType,
    IReadOnlyCollection<Type> SubTypes,
    AutoProvisionMetadata AutoProvisionMetadata
);
