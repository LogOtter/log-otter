using System.Collections.Immutable;

namespace LogOtter.CosmosDb.EventStore.Metadata;

public interface IEventSourceMetadata
{
    Type EventBaseType { get; }
    string EventContainerName { get; }
    ImmutableList<Type> EventTypes { get; }
    ISerializationTypeMap SerializationTypeMap { get; }
    ImmutableList<IProjectionMetadata> Projections { get; }

    ImmutableList<ICatchUpSubscriptionMetadata> CatchUpSubscriptions { get; }
}
