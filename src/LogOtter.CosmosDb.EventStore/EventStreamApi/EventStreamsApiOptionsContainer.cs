namespace LogOtter.CosmosDb.EventStore.EventStreamApi;

internal class EventStreamsApiOptionsContainer
{
    public EventStreamsApiOptions Options { get; private set; }

    public EventStreamsApiOptionsContainer()
    {
        Options = new EventStreamsApiOptions();
    }

    internal void UpdateOptions(EventStreamsApiOptions options)
    {
        Options = options;
    }
}
