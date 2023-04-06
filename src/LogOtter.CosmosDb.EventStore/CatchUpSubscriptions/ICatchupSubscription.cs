namespace LogOtter.CosmosDb.EventStore;

public interface ICatchupSubscription<TBaseEvent>
    where TBaseEvent : class
{
    Task ProcessEvents(IReadOnlyCollection<Event<TBaseEvent>> events, CancellationToken cancellationToken);
}
