namespace LogOtter.CosmosDb.EventStore;

public class EventStoreDependency<TBaseEvent>
{
    public EventStore EventStore { get; }

    public EventStoreDependency(EventStore eventStore)
    {
        EventStore = eventStore;
    }
}