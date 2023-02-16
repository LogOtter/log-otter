using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal interface IHandler
{
    string RequestMethod { get; }
    string Template { get; }
    void SetOptions(EventStreamsApiOptions options);
    Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues);
}
