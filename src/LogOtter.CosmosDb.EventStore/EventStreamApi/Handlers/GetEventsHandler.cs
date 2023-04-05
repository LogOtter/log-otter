using System.Net;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using LogOtter.CosmosDb.EventStore.Metadata;
using LogOtter.JsonHal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventsHandler : BaseHandler
{
    private readonly ICosmosContainerFactory _containerFactory;
    private readonly EventDescriptionGenerator _eventDescriptionGenerator;
    private readonly EventStoreCatalog _eventStoreCatalog;
    private readonly EventStoreOptions _eventStoreOptions;
    private readonly IFeedIteratorFactory _feedIteratorFactory;

    public override string Template => "/event-streams/{EventStreamName}/streams/{StreamId}/events";

    public GetEventsHandler(
        EventStoreCatalog eventStoreCatalog,
        EventDescriptionGenerator eventDescriptionGenerator,
        ICosmosContainerFactory containerFactory,
        IFeedIteratorFactory feedIteratorFactory,
        IOptions<EventStoreOptions> eventStoreOptions
    )
    {
        _eventStoreCatalog = eventStoreCatalog;
        _eventDescriptionGenerator = eventDescriptionGenerator;
        _containerFactory = containerFactory;
        _feedIteratorFactory = feedIteratorFactory;
        _eventStoreOptions = eventStoreOptions.Value;
    }

    public override async Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues)
    {
        var eventStreamName = Uri.UnescapeDataString((string)routeValues["EventStreamName"]!);
        var streamId = _eventStoreOptions.EscapeIdIfRequired(Uri.UnescapeDataString((string)routeValues["StreamId"]!));

        var metaData = _eventStoreCatalog.GetMetadata(eventStreamName);

        if (metaData == null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var page = httpContext.Request.GetPage();

        var container = _containerFactory.GetContainer(metaData.EventContainerName);

        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(streamId) };

        var query = container.GetItemLinqQueryable<CosmosDbStorageEvent>(requestOptions: requestOptions).Where(e => e.StreamId == streamId);

        var itemsQuery = query.OrderByDescending(e => e.EventNumber).Page(page, EventStreamsApiMiddleware.PageSize);

        var totalEvents = await query.CountAsync();

        var storageEvents = new List<CosmosDbStorageEvent>();
        var feedIterator = _feedIteratorFactory.GetFeedIterator(itemsQuery);
        while (feedIterator.HasMoreResults)
        {
            var items = await feedIterator.ReadNextAsync();
            storageEvents.AddRange(items.Resource);
        }

        var events = storageEvents
            .Select(
                e =>
                    new Event(
                        e.Id,
                        e.StreamId,
                        e.BodyType,
                        e.EventNumber,
                        e.EventId,
                        _eventDescriptionGenerator.GetDescription(e, metaData),
                        e.CreatedOn
                    )
            )
            .ToList();

        var response = new EventsResponse(events);
        var prefix = httpContext.Request.GetHost() + Options.RoutePrefix.Value!.TrimEnd('/');

        response.Links.AddPagedLinks(
            page,
            PageHelpers.CalculatePageCount(EventStreamsApiMiddleware.PageSize, totalEvents.Resource),
            p => $"{prefix}/{Uri.EscapeDataString(eventStreamName)}/{Uri.EscapeDataString(streamId)}/events?page={p}"
        );

        await WriteJson(httpContext.Response, response);
    }
}
