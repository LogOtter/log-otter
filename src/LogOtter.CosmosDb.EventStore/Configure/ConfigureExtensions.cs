using LogOtter.CosmosDb;
using LogOtter.CosmosDb.EventStore;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureExtensions
{
    public static IEventStoreBuilder AddEventStore(
        this ICosmosDbBuilder cosmosDbBuilder,
        Action<EventStoreOptions>? setupAction = null
    )
    {
        if (setupAction != null)
        {
            cosmosDbBuilder.Services.Configure(setupAction);
        }
        
        return new EventStoreBuilder(cosmosDbBuilder);
    }
}