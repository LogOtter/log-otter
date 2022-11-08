namespace LogOtter.EventStore.CosmosDb;

public interface ICatchupSubscription<TBaseEvent>
{
    Task ProcessEvents(IReadOnlyCollection<Event<TBaseEvent>> events, CancellationToken cancellationToken);
}