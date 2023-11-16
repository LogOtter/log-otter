namespace LogOtter.CosmosDb.EventStore.EventStreamApi;

internal class EventStreamsApiOptionsContainer
{
    public EventStreamsApiOptions Options { get; private set; } = new();

    internal void UpdateOptions(EventStreamsApiOptions options)
    {
        Options = options;
    }
}
