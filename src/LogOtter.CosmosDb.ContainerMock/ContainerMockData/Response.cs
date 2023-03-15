namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

internal class Response
{
    public ContainerItem Item { get; }
    public bool IsUpdate { get; }

    public Response(ContainerItem item, bool isUpdate)
    {
        Item = item;
        IsUpdate = isUpdate;
    }
}
