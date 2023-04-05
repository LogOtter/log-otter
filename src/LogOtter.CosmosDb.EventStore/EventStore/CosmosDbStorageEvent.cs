using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogOtter.CosmosDb.EventStore;

#pragma warning disable CS8618

public class CosmosDbStorageEvent
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("eventId")]
    public Guid EventId { get; set; }

    [JsonProperty("body")]
    public JObject Body { get; set; }

    [JsonProperty("bodyType")]
    public string BodyType { get; set; }

    [JsonProperty("metadata")]
    public Dictionary<string, string> Metadata { get; set; }

    [JsonProperty("createdOn")]
    public DateTimeOffset CreatedOn { get; set; }

    [JsonProperty("streamId")]
    public string StreamId { get; set; }

    [JsonProperty("eventNumber")]
    public int EventNumber { get; set; }

    internal static CosmosDbStorageEvent FromStorageEvent(StorageEvent storageEvent, SimpleSerializationTypeMap typeMap, JsonSerializer serializer)
    {
        var cosmosDbStorageEvent = new CosmosDbStorageEvent
        {
            Id = $"{storageEvent.StreamId}:{storageEvent.EventNumber}",
            EventId = storageEvent.EventId,
            Body = JObject.FromObject(storageEvent.EventBody, serializer),
            BodyType = typeMap.GetNameFromType(storageEvent.EventBody.GetType()),
            StreamId = storageEvent.StreamId,
            EventNumber = storageEvent.EventNumber,
            Metadata = storageEvent.Metadata
        };

        return cosmosDbStorageEvent;
    }

    public static CosmosDbStorageEvent FromDocument(ItemResponse<CosmosDbStorageEvent> document)
    {
        return document.Resource;
    }

    internal StorageEvent ToStorageEvent(SimpleSerializationTypeMap typeMap, JsonSerializer serializer)
    {
        var bodyType = typeMap.GetTypeFromName(BodyType);
        var body = Body.ToObject(bodyType, serializer);

        return new StorageEvent(StreamId, new EventData(EventId, body, CreatedOn, Metadata), EventNumber);
    }
}
