using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore;

internal class EventStoreMetadata
{
    public Type EventBaseType { get; }
    public Type SnapshotType { get; }
    public string EventContainerName { get; }
    public IReadOnlyCollection<Type> EventTypes { get; }
    public ISerializationTypeMap SerializationTypeMap { get; }
    public JsonSerializerSettings? JsonSerializerSettings { get; }

    public EventStoreMetadata(
        Type eventBaseType, 
        Type snapshotType, 
        string eventContainerName,
        IReadOnlyCollection<Type> eventTypes,
        ISerializationTypeMap serializationTypeMap,
        JsonSerializerSettings? jsonSerializerSettings
    )
    {
        EventBaseType = eventBaseType;
        SnapshotType = snapshotType;
        EventContainerName = eventContainerName;
        EventTypes = eventTypes;
        SerializationTypeMap = serializationTypeMap;
        JsonSerializerSettings = jsonSerializerSettings;
    }
}