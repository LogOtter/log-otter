using System.Text.Json.Serialization;
using LogOtter.JsonHal;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;

internal class EventStreamsResponse
{
    public IList<EventStreamDefinition> Definitions { get; }

    [JsonPropertyName("_links")]
    public JsonHalLinkCollection Links { get; }

    public EventStreamsResponse(IList<EventStreamDefinition> definitions)
    {
        Definitions = definitions;
        Links = new JsonHalLinkCollection();
    }
}