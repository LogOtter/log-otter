using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogOtter.CosmosDb.EventStore;

public class StorageEventJsonConverter<TBaseEvent> : JsonConverter
    where TBaseEvent : class
{
    private readonly SimpleSerializationTypeMap _serializationTypeMap;

    internal StorageEventJsonConverter(SimpleSerializationTypeMap serializationTypeMap)
    {
        _serializationTypeMap = serializationTypeMap;
    }

    public record CosmosDbStorageEvent(
        [property: JsonProperty("id")] string Id,
        string StreamId,
        string BodyType,
        JObject EventBody,
        Dictionary<string, string> Metadata,
        int EventNumber,
        Guid EventId,
        DateTimeOffset CreatedOn
    );

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var storageEvent = (StorageEvent<TBaseEvent>)value;

        var extendedEvent = new CosmosDbStorageEvent(
            storageEvent.StreamId + ":" + storageEvent.EventNumber,
            storageEvent.StreamId,
            _serializationTypeMap.GetNameFromType(storageEvent.EventBody.GetType()),
            JObject.FromObject(storageEvent.EventBody, serializer),
            storageEvent.Metadata,
            storageEvent.EventNumber,
            storageEvent.EventId,
            storageEvent.CreatedOn
        );

        serializer.Serialize(writer, extendedEvent);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var storageEvent = serializer.Deserialize<CosmosDbStorageEvent>(reader);
        var body = (TBaseEvent)storageEvent.EventBody.ToObject(_serializationTypeMap.GetTypeFromName(storageEvent.BodyType), serializer);
        return new StorageEvent<TBaseEvent>(
            storageEvent.StreamId,
            new EventData<TBaseEvent>(storageEvent.EventId, body, storageEvent.CreatedOn, storageEvent.Metadata),
            storageEvent.EventNumber
        );
    }

    public override bool CanConvert(Type objectType) => objectType == typeof(StorageEvent<TBaseEvent>);
}
