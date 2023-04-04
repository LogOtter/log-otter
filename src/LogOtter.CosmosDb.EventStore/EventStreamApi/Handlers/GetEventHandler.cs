using System.Net;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using LogOtter.CosmosDb.EventStore.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventHandler : BaseHandler
{
    private readonly ICosmosContainerFactory _containerFactory;
    private readonly EventDescriptionGenerator _eventDescriptionGenerator;
    private readonly EventStoreCatalog _eventStoreCatalog;
    private readonly EventStoreOptions _eventStoreOptions;
    private readonly IFeedIteratorFactory _feedIteratorFactory;

    public override string Template => "/event-streams/{EventStreamName}/streams/{StreamId}/events/{EventId}";

    public GetEventHandler(
        EventStoreCatalog eventStoreCatalog,
        EventDescriptionGenerator eventDescriptionGenerator,
        IFeedIteratorFactory feedIteratorFactory,
        ICosmosContainerFactory containerFactory,
        IOptions<EventStoreOptions> eventStoreOptions
    )
    {
        _eventStoreCatalog = eventStoreCatalog;
        _eventDescriptionGenerator = eventDescriptionGenerator;
        _feedIteratorFactory = feedIteratorFactory;
        _containerFactory = containerFactory;
        _eventStoreOptions = eventStoreOptions.Value;
    }

    public override async Task HandleRequest(HttpContext httpContext, RouteValueDictionary routeValues)
    {
        var eventStreamName = Uri.UnescapeDataString((string)routeValues["EventStreamName"]!);
        var streamId = _eventStoreOptions.EscapeIdIfRequired(Uri.UnescapeDataString((string)routeValues["StreamId"]!));
        var eventId = Uri.UnescapeDataString((string)routeValues["EventId"]!);

        var metaData = _eventStoreCatalog.GetMetadata(eventStreamName);

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

        var container = _containerFactory.GetContainer(metaData.EventContainerName);

        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(streamId) };

        var query = container
            .GetItemLinqQueryable<IStorageEvent>(requestOptions: requestOptions)
            .Where(e => e.StreamId == streamId && e.EventId == eventIdGuid);

        var storageEvent = (IStorageEvent?)null;

        var feedIterator = _feedIteratorFactory.GetFeedIterator(query);

        while (feedIterator.HasMoreResults)
        {
            var items = await feedIterator.ReadNextAsync();
            storageEvent = items.FirstOrDefault();
            if (storageEvent != null)
            {
                break;
            }
        }

        if (storageEvent == null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var @event = new Event(
            storageEvent.EventId.ToString(),
            storageEvent.StreamId,
            storageEvent.EventBody.GetType().Name,
            storageEvent.EventNumber,
            storageEvent.EventId,
            _eventDescriptionGenerator.GetDescription(storageEvent, metaData),
            storageEvent.CreatedOn
        );

        await WriteJson(httpContext.Response, @event);
    }
}
