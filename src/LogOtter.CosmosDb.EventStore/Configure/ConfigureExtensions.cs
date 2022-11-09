using LogOtter.CosmosDb;
using LogOtter.CosmosDb.EventStore;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureExtensions
{
    public static IEventStoreBuilder AddEventStore(this ICosmosDbBuilder cosmosDbBuilder)
    {
        return new EventStoreBuilder(cosmosDbBuilder);
    }
}