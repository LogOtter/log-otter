using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventStreamHandler : BaseHandler
{
    private readonly EventStoreCatalog _eventStoreCatalog;

    public GetEventStreamHandler(EventStoreCatalog eventStoreCatalog)
    {
        _eventStoreCatalog = eventStoreCatalog;
    }

    public override string Template => "/event-streams/{EventStreamName}";

    public override async Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues)
    {
        var eventStreamName = Uri.UnescapeDataString((string)routeValues["EventStreamName"]!);

        var definition = _eventStoreCatalog.GetDefinition(eventStreamName);

        if (definition == null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        await WriteJson(httpContext.Response, definition);
    }
}
