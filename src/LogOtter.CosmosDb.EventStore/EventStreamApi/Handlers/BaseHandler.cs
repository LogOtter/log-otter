using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal abstract class BaseHandler : IHandler
{
    protected EventStreamsApiOptions Options { get; private set; } = null!;
    
    protected abstract Regex PathRegex { get; }
    protected virtual string RequestMethod { get; } = "GET";

    public void SetOptions(EventStreamsApiOptions options)
    {
        Options = options;
    }
    
    public async Task<bool> HandleRequest(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var response = httpContext.Response;
        var path = request.Path.Value!.TrimEnd('/') + "/";

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

        if (string.Equals(request.Method, RequestMethod, StringComparison.InvariantCultureIgnoreCase))
        {
            if (Options.EnableCors)
            {
                SetCorsHeaders(response);
            }

            await Handle(httpContext, match);
            return true;
        }

        if (Options.EnableCors && RequestMethod != "GET")
        {
            if (string.Equals(request.Method, "OPTIONS", StringComparison.InvariantCultureIgnoreCase))
            {
                SetCorsHeaders(response);
                return true;
            }
        }

        return true;
    }

    private void SetCorsHeaders(HttpResponse response)
    {
        var headers = response.Headers;
        
        headers.AccessControlAllowMethods = Options.AccessControlAllowMethods; 
        headers.AccessControlAllowOrigin = Options.AccessControlAllowOrigin;
        headers.AccessControlAllowCredentials = Options.AccessControlAllowCredentials;
        headers.AccessControlAllowHeaders = Options.AccessControlAllowHeaders;
    }

    public abstract Task Handle(HttpContext httpContext, Match match);
}