namespace LogOtter.EventStore.CosmosDb;

public class EventStoreDependency<TBaseEvent>
{
    public EventStore EventStore { get; }

    public EventStoreDependency(EventStore eventStore)
    {
        EventStore = eventStore;
    }
}