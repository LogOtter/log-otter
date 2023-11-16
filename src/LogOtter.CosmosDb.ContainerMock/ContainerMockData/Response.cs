namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

internal class Response(ContainerItem item, bool isUpdate)
{
    public ContainerItem Item { get; } = item;
    public bool IsUpdate { get; } = isUpdate;
}
