namespace LogOtter.CosmosDb.ContainerMock;

public record TestContainerItem<T>(string PartitionKey, string Id, T Document);
