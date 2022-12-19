using System.Reflection;
using System.Text.RegularExpressions;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetVersionHandler : BaseHandler
{
    protected override Regex PathRegex => new(@"^version/$");
    
    public override async Task Handle(HttpContext httpContext, Match match)
    {
        var packageVersion = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;
        
        var response = new VersionResponse(packageVersion, 1);
        
        await httpContext.Response.WriteJsonAsync(response, EventStreamsApiMiddleware.JsonSerializerOptions);
    }
}