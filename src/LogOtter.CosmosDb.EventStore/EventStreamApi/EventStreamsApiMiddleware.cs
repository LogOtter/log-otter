using System.Text.Json;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;
using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi;

internal class EventStreamsApiMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnumerable<IHandler> _handlers;

    public const int PageSize = 20;

    public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
    };

    public EventStreamsApiMiddleware(
        RequestDelegate next,
        EventStreamsApiOptions options,
        IEnumerable<IHandler> handlers
    )
    {
        _next = next;
        _handlers = handlers;

        foreach (var handler in _handlers)
        {
            handler.SetOptions(options);
        }
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        foreach (var handler in _handlers)
        {
            var handled = await handler.HandleRequest(httpContext);
            if (handled)
            {
                return;
            }
        }

        await _next(httpContext);
    }
}