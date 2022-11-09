namespace LogOtter.CosmosDb.Testing;

public class ContainerDataSnapshot
{
    internal string Json { get; }

    internal ContainerDataSnapshot(string json)
    {
        Json = json;
    }
}