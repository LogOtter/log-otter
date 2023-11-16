using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using LogOtter.CosmosDb.EventStore.Metadata;
using LogOtter.JsonHal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventStreamsHandler(EventStoreCatalog eventStoreCatalog) : BaseHandler
{
    public override string Template => "/event-streams";

    public override async Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues)
    {
        var definitions = eventStoreCatalog.GetDefinitions();

        var page = httpContext.Request.GetPage();

        var response = new EventStreamsResponse(
            definitions.Skip((page - 1) * EventStreamsApiMiddleware.PageSize).Take(EventStreamsApiMiddleware.PageSize).ToList()
        );

        var path = Template;

        response
            .Links
            .AddPagedLinks(page, PageHelpers.CalculatePageCount(EventStreamsApiMiddleware.PageSize, definitions.Count), p => $"{path}?page={p}");

        await WriteJson(httpContext.Response, response);
    }
}
