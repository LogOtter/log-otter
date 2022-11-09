namespace LogOtter.CosmosDb.Testing;

public record TestContainerItem<T>(string PartitionKey, string Id, T Document);