using System.Text.Json.Serialization;
using LogOtter.JsonHal;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;

public class EventsResponse(IList<Event> events)
{
    public IList<Event> Events { get; } = events;

    [JsonPropertyName("_links")]
    public JsonHalLinkCollection Links { get; } = new();
}
