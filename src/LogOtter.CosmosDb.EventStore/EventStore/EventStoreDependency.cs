namespace LogOtter.CosmosDb.EventStore;

// ReSharper disable once UnusedTypeParameter
public class EventStoreDependency<TBaseEvent>
{
    public EventStore EventStore { get; }

    public EventStoreDependency(EventStore eventStore)
    {
        EventStore = eventStore;
    }
}
