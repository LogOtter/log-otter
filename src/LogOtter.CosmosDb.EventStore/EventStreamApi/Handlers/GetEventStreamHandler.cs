using System.Net;
using LogOtter.CosmosDb.EventStore.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventStreamHandler(EventStoreCatalog eventStoreCatalog) : BaseHandler
{
    public override string Template => "/event-streams/{EventStreamName}";

    public override async Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues)
    {
        var eventStreamName = Uri.UnescapeDataString((string)routeValues["EventStreamName"]!);

        var definition = eventStoreCatalog.GetDefinition(eventStreamName);

        if (definition == null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        await WriteJson(httpContext.Response, definition);
    }
}
