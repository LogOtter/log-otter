using System.Net;
using System.Text.RegularExpressions;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using LogOtter.JsonHal;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventsHandler : BaseHandler
{
    private readonly EventStoreCatalog _eventStoreCatalog;
    private readonly EventDescriptionGenerator _eventDescriptionGenerator;
    private readonly ICosmosContainerFactory _containerFactory;
    private readonly IFeedIteratorFactory _feedIteratorFactory;
    private readonly EventStoreOptions _eventStoreOptions;

    protected override Regex PathRegex => new(@"^(?<EventStreamName>[^/]+)/(?<StreamId>[^/]+)/events/$");

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

    public override async Task Handle(HttpContext httpContext, Match match)
    {
        var eventStreamName = Uri.UnescapeDataString(match.Groups["EventStreamName"].Value);
        var streamId = _eventStoreOptions.EscapeIdIfRequired(Uri.UnescapeDataString(match.Groups["StreamId"].Value));

        var metaData = _eventStoreCatalog.GetMetadata(eventStreamName);

        if (metaData == null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var page = httpContext.Request.GetPage();

        var container = _containerFactory.GetContainer(metaData.EventContainerName);

        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(streamId)
        };

        var query = container
            .GetItemLinqQueryable<CosmosDbStorageEventWithTimestamp>(requestOptions: requestOptions)
            .Where(e => e.StreamId == streamId);

        var itemsQuery = query
            .OrderByDescending(e => e.EventNumber)
            .Page(page, EventStreamsApiMiddleware.PageSize);

        var totalEvents = await query.CountAsync();

        var storageEvents = new List<CosmosDbStorageEventWithTimestamp>();
        var feedIterator = _feedIteratorFactory.GetFeedIterator(itemsQuery);
        while (feedIterator.HasMoreResults)
        {
            var items = await feedIterator.ReadNextAsync();
            storageEvents.AddRange(items.Resource);
        }

        var events = storageEvents
            .Select(e => new Event(
                e.Id,
                e.StreamId,
                e.BodyType,
                e.MetadataType,
                e.EventNumber,
                e.EventId,
                e.TimeToLive,
                _eventDescriptionGenerator.GetDescription(e, metaData),
                e.Timestamp
            ))
            .ToList();

        var response = new EventsResponse(events);
        var prefix = httpContext.Request.GetHost() + Options.RoutePrefix.TrimEnd('/');

        response.Links.AddPagedLinks(
            page,
            PageHelpers.CalculatePageCount(EventStreamsApiMiddleware.PageSize, totalEvents.Resource),
            p => $"{prefix}/{Uri.EscapeDataString(eventStreamName)}/{Uri.EscapeDataString(streamId)}/events?page={p}"
        );

        await httpContext.Response.WriteJsonAsync(response, EventStreamsApiMiddleware.JsonSerializerOptions);
    }
}