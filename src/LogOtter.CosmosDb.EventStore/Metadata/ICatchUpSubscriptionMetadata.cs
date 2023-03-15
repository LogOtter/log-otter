namespace LogOtter.CosmosDb.EventStore.Metadata;

public interface ICatchUpSubscriptionMetadata
{
    string ProjectorName { get; }

    Type HandlerType { get; }
}
