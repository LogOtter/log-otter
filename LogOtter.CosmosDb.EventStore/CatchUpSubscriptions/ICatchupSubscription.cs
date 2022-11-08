namespace LogOtter.CosmosDb.EventStore;

public interface ICatchupSubscription<TBaseEvent>
{
    Task ProcessEvents(IReadOnlyCollection<Event<TBaseEvent>> events, CancellationToken cancellationToken);
}