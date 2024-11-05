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

    internal static CosmosDbStorageEvent FromStorageEvent(IStorageEvent storageEvent, SimpleSerializationTypeMap typeMap, JsonSerializer serializer)
    {
        var cosmosDbStorageEvent = new CosmosDbStorageEvent
        {
            Id = $"{storageEvent.StreamId}:{storageEvent.EventNumber}",
            EventId = storageEvent.EventId,
            Body = JObject.FromObject(storageEvent.EventBody, serializer),
            BodyType = typeMap.GetNameFromType(storageEvent.EventBody.GetType()),
            StreamId = storageEvent.StreamId,
            EventNumber = storageEvent.EventNumber,
            Metadata = storageEvent.Metadata,
            CreatedOn = storageEvent.CreatedOn,
        };

        return cosmosDbStorageEvent;
    }

    public static CosmosDbStorageEvent FromDocument(ItemResponse<CosmosDbStorageEvent> document)
    {
        return document.Resource;
    }

    internal StorageEvent<TBaseEvent> ToStorageEvent<TBaseEvent>(SimpleSerializationTypeMap typeMap, JsonSerializer serializer)
        where TBaseEvent : class
    {
        var bodyType = typeMap.GetTypeFromName(BodyType);
        var body = Body.ToObject(bodyType, serializer);

        return new StorageEvent<TBaseEvent>(StreamId, new EventData<TBaseEvent>(EventId, (TBaseEvent)body, CreatedOn, Metadata), EventNumber);
    }
}
