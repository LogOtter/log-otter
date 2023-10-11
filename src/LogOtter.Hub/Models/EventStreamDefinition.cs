using System.Text.Json.Serialization;
using LogOtter.JsonHal;

namespace LogOtter.Hub.Models;

public record EventStreamDefinition(string Name, string TypeName, string ServiceName);

public class EventStreamsResponse
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
