using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal interface IHandler
{
    void SetOptions(EventStreamsApiOptions options);
    Task<bool> HandleRequest(HttpContext httpContext);
}