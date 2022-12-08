using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventStreamHandler : BaseHandler
{
    private readonly EventStoreCatalog _eventStoreCatalog;

    public GetEventStreamHandler(EventStoreCatalog eventStoreCatalog)
    {
        _eventStoreCatalog = eventStoreCatalog;
    }

    protected override Regex PathRegex => new(@"^(?<EventStreamName>[^/]+)/$");

    public override async Task Handle(HttpContext httpContext, Match match)
    {
        var eventStreamName = Uri.UnescapeDataString(match.Groups["EventStreamName"].Value);
        
        var definition = _eventStoreCatalog.GetDefinition(eventStreamName);

        if (definition == null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        await httpContext.Response.WriteJsonAsync(definition, EventStreamsApiMiddleware.JsonSerializerOptions);
    }
}