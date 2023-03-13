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

    public PathString RoutePrefix { get; }

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

        RoutePrefix = _options.RoutePrefix;

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
                continue;
            }

            if (map.Template.TryMatch(remaining, routeValues))
            {
                await map.Handler.HandleRequest(httpContext, routeValues);

                return;
            }
        }

        await _next(httpContext);
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
