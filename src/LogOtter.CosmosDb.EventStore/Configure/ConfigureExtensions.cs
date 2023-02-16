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
        var services = cosmosDbBuilder.Services;

        if (setupAction != null)
        {
            services.Configure(setupAction);
        }

        services.AddSingleton<EventStoreCatalog>();
        services.AddSingleton<EventDescriptionGenerator>();
        services.AddSingleton<IHandler, GetEventStreamsHandler>();
        services.AddSingleton<IHandler, GetEventStreamHandler>();
        services.AddSingleton<IHandler, GetEventsHandler>();
        services.AddSingleton<IHandler, GetEventHandler>();
        services.AddSingleton<IHandler, GetEventBodyHandler>();
        services.AddSingleton<IHandler, GetVersionHandler>();

        services.AddSingleton<EventStreamsApiOptionsContainer>();

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
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var optionsContainer = scope.ServiceProvider.GetRequiredService<EventStreamsApiOptionsContainer>();
            optionsContainer.UpdateOptions(options);
        }

        return app.MapWhen(c => c.Request.Path.StartsWithSegments(options.RoutePrefix), a => a.UseMiddleware<EventStreamsApiMiddleware>(options));
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
