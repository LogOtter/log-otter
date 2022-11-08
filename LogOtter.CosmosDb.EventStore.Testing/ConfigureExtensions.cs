using LogOtter.CosmosDb.Testing;

namespace LogOtter.CosmosDb.EventStore.Testing;

public static class ConfigureExtensions
{
    public static ITestEventStoreBuilder AddTestEventStore(this ITestCosmosDbBuilder cosmosDbBuilder)
    {
        return new TestEventStoreBuilder(cosmosDbBuilder.Services);
    }
}