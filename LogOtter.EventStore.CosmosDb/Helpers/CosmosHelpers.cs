namespace LogOtter.EventStore.CosmosDb;

public static class CosmosHelpers
{
    public static string EscapeForCosmosId(string id)
    {
        return id
            .Replace("/", "|")
            .Replace(@"\", "|")
            .Replace("?", ":")
            .Replace("#", ":");
    }
}