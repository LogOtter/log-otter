using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore;

internal interface IEventStoreMetadata
{
    Type EventBaseType { get; }
    Type SnapshotType { get; }
    string EventContainerName { get; }
    IReadOnlyCollection<Type> EventTypes { get; }
    ISerializationTypeMap SerializationTypeMap { get; }
    JsonSerializerSettings? JsonSerializerSettings { get; }
}
