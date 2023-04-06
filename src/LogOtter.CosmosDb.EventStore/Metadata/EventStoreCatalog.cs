using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.EventStore.Metadata;

internal class EventStoreCatalog
{
    private readonly Lazy<IReadOnlyCollection<EventStreamDefinition>> _definitions;
    private readonly IReadOnlyCollection<IEventSourceMetadata> _eventStoreMetaData;
    private readonly IServiceProvider _serviceProvider;

    public EventStoreCatalog(IEnumerable<IEventSourceMetadata> eventStoreMetaData, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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

    public IEventSourceMetadata? GetMetadata(string name)
    {
        return _eventStoreMetaData.FirstOrDefault(e => string.Equals(e.EventBaseType.Name, name, StringComparison.InvariantCultureIgnoreCase));
    }

    public IEventStoreReader GetEventStoreReader(IEventSourceMetadata metadata)
    {
        return (IEventStoreReader)_serviceProvider.GetRequiredService(metadata.EventStoreBaseType);
    }

    private IReadOnlyCollection<EventStreamDefinition> GenerateDefinitions()
    {
        return _eventStoreMetaData.Select(e => new EventStreamDefinition(e.EventBaseType.Name, e.EventBaseType.FullName!)).ToList().AsReadOnly();
    }
}
