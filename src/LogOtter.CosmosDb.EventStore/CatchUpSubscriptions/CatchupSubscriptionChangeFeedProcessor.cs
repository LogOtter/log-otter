namespace LogOtter.CosmosDb.EventStore;

public class CatchupSubscriptionChangeFeedProcessor<TBaseEvent, TCatchupSubscriptionHandler> : IChangeFeedProcessorChangeHandler<Event<TBaseEvent>>
    where TCatchupSubscriptionHandler : ICatchupSubscription<TBaseEvent>
{
    private readonly ICatchupSubscription<TBaseEvent> _catchupSubscription;

    public CatchupSubscriptionChangeFeedProcessor(TCatchupSubscriptionHandler catchupSubscription)
    {
        _catchupSubscription = catchupSubscription;
    }

    public async Task ProcessChanges(IReadOnlyCollection<Event<TBaseEvent>> changes, CancellationToken cancellationToken)
    {
        await _catchupSubscription.ProcessEvents(changes, cancellationToken);
    }
}
