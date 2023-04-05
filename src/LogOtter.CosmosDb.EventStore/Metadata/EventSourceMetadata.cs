using System.Collections.Immutable;

namespace LogOtter.CosmosDb.EventStore.Metadata;

internal record EventSourceMetadata<TBaseEvent>(
    string EventContainerName,
    ImmutableList<Type> EventTypes,
    SimpleSerializationTypeMap SerializationTypeMap,
    ImmutableList<IProjectionMetadata> Projections,
    ImmutableList<ICatchUpSubscriptionMetadata> CatchUpSubscriptions
) : IEventSourceMetadata
{
    Type IEventSourceMetadata.EventBaseType => typeof(TBaseEvent);
}
