using System.Text.Json.Serialization;
using LogOtter.JsonHal;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;

internal class EventStreamsResponse(IList<EventStreamDefinition> definitions)
{
    public IList<EventStreamDefinition> Definitions { get; } = definitions;

    [JsonPropertyName("_links")]
    public JsonHalLinkCollection Links { get; } = new();
}
