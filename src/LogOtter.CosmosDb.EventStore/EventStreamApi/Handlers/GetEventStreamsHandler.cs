using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using LogOtter.JsonHal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventStreamsHandler : BaseHandler
{
    private readonly EventStoreCatalog _eventStoreCatalog;

    public override string Template => "/event-streams";

    public GetEventStreamsHandler(EventStoreCatalog eventStoreCatalog)
    {
        _eventStoreCatalog = eventStoreCatalog;
    }

    public override async Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues)
    {
        var definitions = _eventStoreCatalog.GetDefinitions();

        var page = httpContext.Request.GetPage();

        var response = new EventStreamsResponse(definitions
            .Skip((page - 1) * EventStreamsApiMiddleware.PageSize)
            .Take(EventStreamsApiMiddleware.PageSize)
            .ToList()
        );

        var prefix = httpContext.Request.GetHost() + Options.RoutePrefix.Value!.TrimEnd('/');

        response.Links.AddPagedLinks(
            page,
            PageHelpers.CalculatePageCount(EventStreamsApiMiddleware.PageSize, definitions.Count),
            p => $"{prefix}?page={p}"
        );

        await WriteJson(httpContext.Response, response);
    }
}
