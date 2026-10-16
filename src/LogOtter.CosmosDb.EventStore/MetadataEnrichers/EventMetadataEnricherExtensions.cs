using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogOtter.CosmosDb.EventStore;

public static class EventMetadataEnricherExtensions
{
    /// <summary>
    /// Registers an <see cref="IEventMetadataEnricher"/> that contributes metadata to events as they are appended.
    /// Enrichers are opt-in: none are registered by default. Registering the same enricher type more than once is a no-op.
    /// </summary>
    public static IServiceCollection AddEventMetadataEnricher<TEnricher>(this IServiceCollection services)
        where TEnricher : class, IEventMetadataEnricher
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventMetadataEnricher, TEnricher>());
        return services;
    }
}
