using System.Text.Json.Serialization;
using LogOtter.JsonHal;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;

public class EventsResponse
{
    public IList<Event> Events { get; }

    [JsonPropertyName("_links")]
    public JsonHalLinkCollection Links { get; }

    public EventsResponse(IList<Event> events)
    {
        Events = events;
        Links = new JsonHalLinkCollection();
    }
}
