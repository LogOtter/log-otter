using LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi;

internal class EventStreamsApiMiddleware
{
    private readonly RequestDelegate _next;
    private readonly EventStreamsApiOptions _options;
    private readonly IEnumerable<(IHandler Handler, TemplateMatcher Template)> _handlerMap;

    public const int PageSize = 20;

    public EventStreamsApiMiddleware(
        RequestDelegate next,
        EventStreamsApiOptions options,
        IEnumerable<IHandler> handlers
    )
    {
        _next = next;
        _options = options;
        _handlerMap = handlers
            .Select(h => (h, BuildTemplateMatcher(h.Template)))
            .ToList();

        foreach (var map in _handlerMap)
        {
            map.Handler.SetOptions(options);
        }
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var path = new PathString(request.Path.Value);

        if (!path.StartsWithSegments(_options.RoutePrefix, out var remaining))
        {
            await _next(httpContext);
            return;
        }

        foreach (var map in _handlerMap)
        {
            var routeValues = new RouteValueDictionary();

            if (request.Method != map.Handler.RequestMethod)
            {
                if (_options.EnableCors &&
                    string.Equals(request.Method, "OPTIONS", StringComparison.InvariantCultureIgnoreCase) &&
                    !string.Equals(map.Handler.RequestMethod, "GET", StringComparison.InvariantCultureIgnoreCase) &&
                    map.Template.TryMatch(remaining, routeValues))
                {
                    SetCorsHeaders(httpContext.Response);
                }

                continue;
            }

            if (map.Template.TryMatch(remaining, routeValues))
            {
                if (_options.EnableCors)
                {
                    SetCorsHeaders(httpContext.Response);
                }

                await map.Handler.HandleRequest(httpContext, routeValues);

                return;
            }
        }

        await _next(httpContext);
    }

    private void SetCorsHeaders(HttpResponse response)
    {
        var headers = response.Headers;

        headers.AccessControlAllowMethods = _options.AccessControlAllowMethods;
        headers.AccessControlAllowOrigin = _options.AccessControlAllowOrigin;
        headers.AccessControlAllowCredentials = _options.AccessControlAllowCredentials;
        headers.AccessControlAllowHeaders = _options.AccessControlAllowHeaders;
    }

    private static TemplateMatcher BuildTemplateMatcher(string template)
    {
        var defaultValues = new RouteValueDictionary();

        var routeTemplate = TemplateParser.Parse(template);

        foreach (var parameter in routeTemplate.Parameters.Where(p => p.DefaultValue != null))
        {
            defaultValues.Add(parameter.Name!, parameter.DefaultValue);
        }

        return new TemplateMatcher(routeTemplate, defaultValues);
    }
}
