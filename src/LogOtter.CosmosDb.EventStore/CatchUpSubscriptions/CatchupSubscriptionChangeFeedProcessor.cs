namespace LogOtter.CosmosDb.EventStore;

public class CatchupSubscriptionChangeFeedProcessor<TBaseEvent, TCatchupSubscriptionHandler>(TCatchupSubscriptionHandler catchupSubscription)
    : IChangeFeedProcessorChangeHandler<Event<TBaseEvent>>
    where TCatchupSubscriptionHandler : ICatchupSubscription<TBaseEvent>
    where TBaseEvent : class
{
    private readonly ICatchupSubscription<TBaseEvent> _catchupSubscription = catchupSubscription;

    public async Task ProcessChanges(IReadOnlyCollection<Event<TBaseEvent>> changes, CancellationToken cancellationToken)
    {
        await _catchupSubscription.ProcessEvents(changes, cancellationToken);
    }
}
