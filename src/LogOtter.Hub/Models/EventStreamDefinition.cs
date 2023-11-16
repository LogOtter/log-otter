using System.Text.Json.Serialization;
using LogOtter.JsonHal;

namespace LogOtter.Hub.Models;

public record EventStreamDefinition(string Name, string TypeName, string ServiceName);

public class EventStreamsResponse(IList<EventStreamDefinition> definitions)
{
    public IList<EventStreamDefinition> Definitions { get; } = definitions;

    [JsonPropertyName("_links")]
    public JsonHalLinkCollection Links { get; } = new();
}
