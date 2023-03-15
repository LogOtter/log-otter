using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore;

public class EventStoreMetadata<TBaseEvent, TSnapshot> : IEventStoreMetadata
{
    Type IEventStoreMetadata.EventBaseType => typeof(TBaseEvent);
    Type IEventStoreMetadata.SnapshotType => typeof(TSnapshot);
    public string EventContainerName { get; }
    public IReadOnlyCollection<Type> EventTypes { get; }
    public ISerializationTypeMap SerializationTypeMap { get; }
    public JsonSerializerSettings? JsonSerializerSettings { get; }

    public Func<TBaseEvent, string> SnapshotPartitionKeyResolver { get; }

    public EventStoreMetadata(
        string eventContainerName,
        IReadOnlyCollection<Type> eventTypes,
        ISerializationTypeMap serializationTypeMap,
        JsonSerializerSettings? jsonSerializerSettings,
        Func<TBaseEvent, string> snapshotPartitionKeyResolver)
    {
        EventContainerName = eventContainerName;
        EventTypes = eventTypes;
        SerializationTypeMap = serializationTypeMap;
        JsonSerializerSettings = jsonSerializerSettings;
        SnapshotPartitionKeyResolver = snapshotPartitionKeyResolver;
    }
}
