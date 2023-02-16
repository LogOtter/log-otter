using System.Reflection;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetVersionHandler : BaseHandler
{
    public override string Template => "/version";

    public override async Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues)
    {
        var packageVersion = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;

        var response = new VersionResponse(packageVersion, 1);

        await WriteJson(httpContext.Response, response);
    }
}
