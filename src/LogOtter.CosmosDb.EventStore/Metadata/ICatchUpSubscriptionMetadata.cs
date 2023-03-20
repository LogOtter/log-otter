namespace LogOtter.CosmosDb.EventStore.Metadata;

internal interface ICatchUpSubscriptionMetadata
{
    string ProjectorName { get; }

    Type HandlerType { get; }
}
