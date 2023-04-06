using System.Collections.Immutable;

namespace LogOtter.CosmosDb.EventStore.Metadata;

internal interface IEventSourceMetadata
{
    Type EventBaseType { get; }
    Type EventStoreBaseType { get; }
    string EventContainerName { get; }
    ImmutableList<Type> EventTypes { get; }
    SimpleSerializationTypeMap SerializationTypeMap { get; }
    ImmutableList<IProjectionMetadata> Projections { get; }
    ImmutableList<ICatchUpSubscriptionMetadata> CatchUpSubscriptions { get; }
}
