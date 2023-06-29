namespace LogOtter.CosmosDb.EventStore;

public class EventualCatchupSubscriptionChangeFeedProcessor<TBaseEvent, TCatchupSubscriptionHandler>
    : IChangeFeedProcessorChangeHandler<EventualEvent<TBaseEvent>>
    where TCatchupSubscriptionHandler : IEventualCatchupSubscription<TBaseEvent>
    where TBaseEvent : class
{
    private readonly IEventualCatchupSubscription<TBaseEvent> _catchupSubscription;

    public EventualCatchupSubscriptionChangeFeedProcessor(TCatchupSubscriptionHandler catchupSubscription)
    {
        _catchupSubscription = catchupSubscription;
    }

    public async Task ProcessChanges(IReadOnlyCollection<EventualEvent<TBaseEvent>> changes, CancellationToken cancellationToken)
    {
        await _catchupSubscription.ProcessEvents(changes, cancellationToken);
    }
}
