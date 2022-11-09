namespace CosmosTestHelpers;

public record TestContainerItem<T>(string PartitionKey, string Id, T Document);