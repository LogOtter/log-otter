namespace LogOtter.CosmosDb.EventStore.Metadata;

internal record CatchUpSubscriptionMetadata<TCatchupSubscriptionHandler>(string ProjectorName) : ICatchUpSubscriptionMetadata
{
    Type ICatchUpSubscriptionMetadata.HandlerType => typeof(TCatchupSubscriptionHandler);
}
