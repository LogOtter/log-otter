namespace CosmosTestHelpers;

public class ContainerDataSnapshot
{
    internal string Json { get; }

    internal ContainerDataSnapshot(string json)
    {
        Json = json;
    }
}