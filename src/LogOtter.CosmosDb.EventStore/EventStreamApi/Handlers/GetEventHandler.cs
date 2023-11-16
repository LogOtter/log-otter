using System.Net;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using LogOtter.CosmosDb.EventStore.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventHandler(
    EventStoreCatalog eventStoreCatalog,
    EventDescriptionGenerator eventDescriptionGenerator,
    IOptions<EventStoreOptions> eventStoreOptions
) : BaseHandler
{
    public override string Template => "/event-streams/{EventStreamName}/streams/{StreamId}/events/{EventId}";

    public override async Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues)
    {
        var eventStreamName = Uri.UnescapeDataString((string)routeValues["EventStreamName"]!);
        var streamId = eventStoreOptions.Value.EscapeIdIfRequired(Uri.UnescapeDataString((string)routeValues["StreamId"]!));
        var eventId = Uri.UnescapeDataString((string)routeValues["EventId"]!);

        var metaData = eventStoreCatalog.GetMetadata(eventStreamName);

        if (metaData == null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        if (!Guid.TryParse(eventId, out var eventIdGuid))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var eventStore = eventStoreCatalog.GetEventStoreReader(metaData);
        var storageEvent = await eventStore.ReadEventFromStream(streamId, eventIdGuid);

        var @event = new Event(
            storageEvent.EventId.ToString(),
            storageEvent.StreamId,
            storageEvent.EventBody.GetType().Name,
            storageEvent.EventNumber,
            storageEvent.EventId,
            eventDescriptionGenerator.GetDescription(storageEvent, metaData),
            storageEvent.CreatedOn
        );

        await WriteJson(httpContext.Response, @event);
    }
}
