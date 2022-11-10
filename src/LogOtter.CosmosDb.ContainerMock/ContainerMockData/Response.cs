namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

internal class Response
{
    public Response(ContainerItem item, bool isUpdate)
    {
        Item = item;
        IsUpdate = isUpdate;
    }
        
    public ContainerItem Item { get; }
    public bool IsUpdate { get; }
}