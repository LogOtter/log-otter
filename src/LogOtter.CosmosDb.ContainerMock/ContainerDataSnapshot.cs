namespace LogOtter.CosmosDb.ContainerMock;

public class ContainerDataSnapshot
{
    internal string Json { get; }

    internal ContainerDataSnapshot(string json)
    {
        Json = json;
    }
}
