using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal abstract class BaseHandler : IHandler
{
    protected abstract Regex PathRegex { get; }
    protected EventStreamsApiOptions Options { get; private set; } = null!;

    public void SetOptions(EventStreamsApiOptions options)
    {
        Options = options;
    }
    
    public async Task<bool> HandleRequest(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var path = request.Path.Value.TrimEnd('/') + "/";

        var routePrefix = Options.RoutePrefix.TrimEnd('/') + "/";
        if (!path.StartsWith(routePrefix, StringComparison.Ordinal))
        {
            return false;
        }
        
        var remainingPath = path[routePrefix.Length..];
        var match = PathRegex.Match(remainingPath);
        
        if (!match.Success)
        {
            return false;
        }
        
        await Handle(httpContext, match);
        return true;
    }

    public abstract Task Handle(HttpContext httpContext, Match match);
}