namespace LogOtter.CosmosDb.EventStore;

internal static class EventStoreOptionsExtensions
{
    public static string EscapeIdIfRequired(this EventStoreOptions options, string rawId)
    {
        if (!options.AutoEscapeIds)
        {
            return rawId;
        }

        return rawId.Replace("/", "|").Replace(@"\", "|").Replace("?", ":").Replace("#", ":");
    }
}
