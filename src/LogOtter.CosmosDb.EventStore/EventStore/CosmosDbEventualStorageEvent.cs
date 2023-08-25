using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogOtter.CosmosDb.EventStore;

#pragma warning disable CS8618

public class CosmosDbEventualStorageEvent
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("eventId")]
    public string EventId { get; set; }

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

    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    internal static CosmosDbEventualStorageEvent FromStorageEvent(
        IEventualStorageEvent storageEvent,
        SimpleSerializationTypeMap typeMap,
        JsonSerializer serializer
    )
    {
        var cosmosDbStorageEvent = new CosmosDbEventualStorageEvent
        {
            Id = $"{storageEvent.StreamId}:{storageEvent.EventId}",
            EventId = storageEvent.EventId,
            Body = JObject.FromObject(storageEvent.EventBody, serializer),
            BodyType = typeMap.GetNameFromType(storageEvent.EventBody.GetType()),
            StreamId = storageEvent.StreamId,
            Timestamp = storageEvent.Timestamp,
            Metadata = storageEvent.Metadata,
            CreatedOn = storageEvent.CreatedOn
        };

        return cosmosDbStorageEvent;
    }

    public static CosmosDbEventualStorageEvent FromDocument(ItemResponse<CosmosDbEventualStorageEvent> document)
    {
        return document.Resource;
    }

    internal EventualStorageEvent<TBaseEvent> ToStorageEvent<TBaseEvent>(SimpleSerializationTypeMap typeMap, JsonSerializer serializer)
        where TBaseEvent : class
    {
        var bodyType = typeMap.GetTypeFromName(BodyType);
        var body = Body.ToObject(bodyType, serializer);

        return new EventualStorageEvent<TBaseEvent>(
            StreamId,
            new EventualEventData<TBaseEvent>(EventId, (TBaseEvent)body, Timestamp, CreatedOn, Metadata)
        );
    }
}
