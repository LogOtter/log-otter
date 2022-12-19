using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Handlers;

internal class GetEventBodyHandler : BaseHandler
{
    private readonly EventStoreCatalog _eventStoreCatalog;
    private readonly IFeedIteratorFactory _feedIteratorFactory;
    private readonly ICosmosContainerFactory _containerFactory;
    private readonly EventStoreOptions _eventStoreOptions;

    protected override Regex PathRegex => new(@"^event-streams/(?<EventStreamName>[^/]+)/streams/(?<StreamId>[^/]+)/events/(?<EventId>[^/]+)/body/$");

    public GetEventBodyHandler(
        EventStoreCatalog eventStoreCatalog,
        IFeedIteratorFactory feedIteratorFactory,
        ICosmosContainerFactory containerFactory,
        IOptions<EventStoreOptions> eventStoreOptions
    )
    {
        _eventStoreCatalog = eventStoreCatalog;
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

        if (!Guid.TryParse(Uri.EscapeDataString(eventId), out var eventIdGuid))
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
            .GetItemLinqQueryable<CosmosDbStorageEvent>(requestOptions: requestOptions)
            .Where(e => e.StreamId == streamId && e.EventId == eventIdGuid);

        var storageEvent = (CosmosDbStorageEvent?)null;

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


        httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
        await httpContext.Response.WriteAsync(storageEvent.Body.ToString());
    }
}