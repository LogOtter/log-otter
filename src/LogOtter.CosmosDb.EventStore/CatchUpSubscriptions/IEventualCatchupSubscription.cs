namespace LogOtter.CosmosDb.EventStore;

public interface IEventualCatchupSubscription<TBaseEvent>
    where TBaseEvent : class
{
    Task ProcessEvents(IReadOnlyCollection<EventualEvent<TBaseEvent>> events, CancellationToken cancellationToken);
}
