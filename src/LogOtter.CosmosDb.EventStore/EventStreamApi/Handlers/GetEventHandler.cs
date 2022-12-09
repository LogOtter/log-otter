using System.Net;
using System.Text.RegularExpressions;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventHandler : BaseHandler
{
    private readonly EventStoreCatalog _eventStoreCatalog;
    private readonly EventDescriptionGenerator _eventDescriptionGenerator;
    private readonly IFeedIteratorFactory _feedIteratorFactory;
    private readonly ICosmosContainerFactory _containerFactory;
    private readonly EventStoreOptions _eventStoreOptions;

    protected override Regex PathRegex => new(@"^(?<EventStreamName>[^/]+)/(?<StreamId>[^/]+)/events/(?<EventId>[^/]+)/$");

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

    public override async Task Handle(HttpContext httpContext, Match match)
    {
        var eventStreamName = Uri.UnescapeDataString(match.Groups["EventStreamName"].Value);
        var streamId = _eventStoreOptions.EscapeIdIfRequired(Uri.UnescapeDataString(match.Groups["StreamId"].Value));
        var eventId = Uri.UnescapeDataString(match.Groups["EventId"].Value);

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

        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(streamId)
        };

        var query = container
            .GetItemLinqQueryable<CosmosDbStorageEventWithTimestamp>(requestOptions: requestOptions)
            .Where(e => e.StreamId == streamId && e.EventId == eventIdGuid);

        var storageEvent = (CosmosDbStorageEventWithTimestamp?)null;
        
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
            storageEvent.Id,
            storageEvent.StreamId,
            storageEvent.BodyType,
            storageEvent.MetadataType,
            storageEvent.EventNumber,
            storageEvent.EventId,
            storageEvent.TimeToLive,
            _eventDescriptionGenerator.GetDescription(storageEvent, metaData),
            storageEvent.Timestamp
        );

        await httpContext.Response.WriteJsonAsync(@event, EventStreamsApiMiddleware.JsonSerializerOptions);
    }
}