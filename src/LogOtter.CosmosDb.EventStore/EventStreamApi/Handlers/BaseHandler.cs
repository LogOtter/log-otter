using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal abstract class BaseHandler : IHandler
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
    };

    protected EventStreamsApiOptions Options { get; private set; } = null!;

    public virtual string RequestMethod => "GET";
    public abstract string Template { get; }

    public void SetOptions(EventStreamsApiOptions options)
    {
        Options = options;
    }

    public abstract Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues);

    protected async Task WriteJson<T>(HttpResponse response, T value)
    {
        await response.WriteAsJsonAsync(value, JsonSerializerOptions);
    }
}
