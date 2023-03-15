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
    public JObject? Metadata { get; set; }

    [JsonProperty("metadataType")]
    public string MetadataType { get; set; }

    [JsonProperty("streamId")]
    public string StreamId { get; set; }

    [JsonProperty("eventNumber")]
    public int EventNumber { get; set; }

    [JsonProperty("ttl")]
    public int TimeToLive { get; set; }

    public static CosmosDbStorageEvent FromStorageEvent(StorageEvent storageEvent, ISerializationTypeMap typeMap, JsonSerializer serializer)
    {
        var cosmosDbStorageEvent = new CosmosDbStorageEvent
        {
            Id = $"{storageEvent.StreamId}:{storageEvent.EventNumber}",
            EventId = storageEvent.EventId,
            Body = JObject.FromObject(storageEvent.EventBody, serializer),
            BodyType = typeMap.GetNameFromType(storageEvent.EventBody.GetType()),
            StreamId = storageEvent.StreamId,
            EventNumber = storageEvent.EventNumber,
            TimeToLive = storageEvent.TimeToLive
        };

        if (storageEvent.Metadata != null)
        {
            cosmosDbStorageEvent.Metadata = JObject.FromObject(storageEvent.Metadata, serializer);
            cosmosDbStorageEvent.MetadataType = typeMap.GetNameFromType(storageEvent.Metadata.GetType());
        }

        return cosmosDbStorageEvent;
    }

    public static CosmosDbStorageEvent FromDocument(ItemResponse<CosmosDbStorageEvent> document)
    {
        return document.Resource;
    }

    public StorageEvent ToStorageEvent(ISerializationTypeMap typeMap, JsonSerializer serializer)
    {
        var bodyType = typeMap.GetTypeFromName(BodyType);
        var body = Body.ToObject(bodyType, serializer);
        var metadata = Metadata?.ToObject(typeMap.GetTypeFromName(MetadataType), serializer);

        return new StorageEvent(
            StreamId,
            new EventData(
                EventId,
                body,
                TimeToLive,
                metadata),
            EventNumber);
    }
}
