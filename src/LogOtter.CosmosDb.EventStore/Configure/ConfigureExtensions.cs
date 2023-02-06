using LogOtter.CosmosDb.EventStore.EventStreamApi;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;
using LogOtter.CosmosDb.EventStore.EventStreamUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore;

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

        cosmosDbBuilder.Services.AddSingleton<EventStoreCatalog>();
        cosmosDbBuilder.Services.AddSingleton<EventDescriptionGenerator>();
        cosmosDbBuilder.Services.AddSingleton<IHandler, GetEventStreamsHandler>();
        cosmosDbBuilder.Services.AddSingleton<IHandler, GetEventStreamHandler>();
        cosmosDbBuilder.Services.AddSingleton<IHandler, GetEventsHandler>();
        cosmosDbBuilder.Services.AddSingleton<IHandler, GetEventHandler>();
        cosmosDbBuilder.Services.AddSingleton<IHandler, GetEventBodyHandler>();
        cosmosDbBuilder.Services.AddSingleton<IHandler, GetVersionHandler>();

        return new EventStoreBuilder(cosmosDbBuilder);
    }

    public static IApplicationBuilder UseEventStreamsApi(
        this IApplicationBuilder app,
        Action<EventStreamsApiOptions>? setupAction = null
    )
    {
        EventStreamsApiOptions options;
        using (var scope = app.ApplicationServices.CreateScope())
        {
            options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<EventStreamsApiOptions>>().Value;
            setupAction?.Invoke(options);
        }

        return app.UseEventStreamsApi(options);
    }

    public static IApplicationBuilder UseEventStreamsApi(this IApplicationBuilder app, EventStreamsApiOptions options)
    {
        return app.UseMiddleware<EventStreamsApiMiddleware>(options);
    }
    
    public static IApplicationBuilder UseEventStreamsUI(
        this IApplicationBuilder app,
        Action<EventStreamsUIOptions>? setupAction = null
    )
    {
        EventStreamsUIOptions options;
        using (var scope = app.ApplicationServices.CreateScope())
        {
            options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<EventStreamsUIOptions>>().Value;
            setupAction?.Invoke(options);
        }

        return app.UseEventStreamsUI(options);
    }
    
    public static IApplicationBuilder UseEventStreamsUI(this IApplicationBuilder app, EventStreamsUIOptions options)
    {
        return app.UseMiddleware<EventStreamsUIMiddleware>(options);
    }
}