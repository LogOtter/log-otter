using System.Net;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using LogOtter.CosmosDb.EventStore.Metadata;
using LogOtter.JsonHal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventsHandler : BaseHandler
{
    private readonly EventDescriptionGenerator _eventDescriptionGenerator;
    private readonly EventStoreCatalog _eventStoreCatalog;
    private readonly EventStoreOptions _eventStoreOptions;

    public override string Template => "/event-streams/{EventStreamName}/streams/{StreamId}/events";

    public GetEventsHandler(
        EventStoreCatalog eventStoreCatalog,
        EventDescriptionGenerator eventDescriptionGenerator,
        IOptions<EventStoreOptions> eventStoreOptions
    )
    {
        _eventStoreCatalog = eventStoreCatalog;
        _eventDescriptionGenerator = eventDescriptionGenerator;
        _eventStoreOptions = eventStoreOptions.Value;
    }

    public override async Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues)
    {
        var eventStreamName = Uri.UnescapeDataString((string)routeValues["EventStreamName"]!);
        var streamId = _eventStoreOptions.EscapeIdIfRequired(Uri.UnescapeDataString((string)routeValues["StreamId"]!));

        var metadata = _eventStoreCatalog.GetMetadata(eventStreamName);
        if (metadata == null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var eventStore = _eventStoreCatalog.GetEventStoreReader(metadata);
        var page = httpContext.Request.GetPage();
        var totalEvents = await eventStore.ReadStreamEventCount(streamId);
        var storageEvents = await eventStore.ReadStreamForwards(
            streamId,
            (page - 1) * EventStreamsApiMiddleware.PageSize,
            EventStreamsApiMiddleware.PageSize
        );

        var events = storageEvents
            .Select(
                e =>
                    new Event(
                        e.EventId.ToString(),
                        e.StreamId,
                        e.EventBody.GetType().Name,
                        e.EventNumber,
                        e.EventId,
                        _eventDescriptionGenerator.GetDescription(e, metadata),
                        e.CreatedOn
                    )
            )
            .ToList();

        var response = new EventsResponse(events);

        var logOtterHubPath = httpContext.Request.GetLogOtterHubPath();
        var prefix = logOtterHubPath ?? httpContext.Request.GetHost() + Options.RoutePrefix.Value!.TrimEnd('/');

        var path = Template.Replace("{EventStreamName}", Uri.EscapeDataString(eventStreamName)).Replace("{StreamId}", Uri.EscapeDataString(streamId));

        response.Links.AddPagedLinks(
            page,
            PageHelpers.CalculatePageCount(EventStreamsApiMiddleware.PageSize, totalEvents),
            p => $"{prefix}{path}?page={p}"
        );

        await WriteJson(httpContext.Response, response);
    }
}
