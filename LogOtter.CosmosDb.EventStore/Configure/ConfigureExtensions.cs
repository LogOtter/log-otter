namespace LogOtter.CosmosDb.EventStore;

public static class ConfigureExtensions
{
    public static IEventStoreBuilder AddEventStore(this ICosmosDbBuilder cosmosDbBuilder)
    {
        return new EventStoreBuilder(cosmosDbBuilder);
    }
}