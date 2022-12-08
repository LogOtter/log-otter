using System.Text.RegularExpressions;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using LogOtter.JsonHal;
using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventStreamsHandler : BaseHandler
{
    private readonly EventStoreCatalog _eventStoreCatalog;

    protected override Regex PathRegex => new(@"^$");

    public GetEventStreamsHandler(EventStoreCatalog eventStoreCatalog)
    {
        _eventStoreCatalog = eventStoreCatalog;
    }

    public override async Task Handle(HttpContext httpContext, Match match)
    {
        var definitions = _eventStoreCatalog.GetDefinitions();

        var page = httpContext.Request.GetPage();

        var response = new EventStreamsResponse(definitions
            .Skip((page - 1) * EventStreamsApiMiddleware.PageSize)
            .Take(EventStreamsApiMiddleware.PageSize)
            .ToList()
        );

        var prefix = httpContext.Request.GetHost() + Options.RoutePrefix.TrimEnd('/');

        response.Links.AddPagedLinks(
            page,
            PageHelpers.CalculatePageCount(EventStreamsApiMiddleware.PageSize, definitions.Count),
            p => $"{prefix}?page={p}"
        );

        await httpContext.Response.WriteJsonAsync(response, EventStreamsApiMiddleware.JsonSerializerOptions);
    }
}