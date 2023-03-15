using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;

namespace LogOtter.CosmosDb.EventStore;

internal class EventStoreCatalog
{
    private readonly Lazy<IReadOnlyCollection<EventStreamDefinition>> _definitions;
    private readonly IReadOnlyCollection<IEventStoreMetadata> _eventStoreMetaData;

    public EventStoreCatalog(IEnumerable<IEventStoreMetadata> eventStoreMetaData)
    {
        _eventStoreMetaData = eventStoreMetaData.ToList();
        _definitions = new Lazy<IReadOnlyCollection<EventStreamDefinition>>(GenerateDefinitions);
    }

    public IReadOnlyCollection<EventStreamDefinition> GetDefinitions()
    {
        return _definitions.Value;
    }

    public EventStreamDefinition? GetDefinition(string name)
    {
        return _definitions.Value.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.InvariantCultureIgnoreCase));
    }

    public IEventStoreMetadata? GetMetadata(string name)
    {
        return _eventStoreMetaData.FirstOrDefault(e => string.Equals(e.EventBaseType.Name, name, StringComparison.InvariantCultureIgnoreCase));
    }

    private IReadOnlyCollection<EventStreamDefinition> GenerateDefinitions()
    {
        return _eventStoreMetaData.Select(e => new EventStreamDefinition(e.EventBaseType.Name, e.EventBaseType.FullName!)).ToList().AsReadOnly();
    }
}
