using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace LogOtter.EventStore.CosmosDb;

internal class EventStoreBuilder : IEventStoreBuilder
{
    private readonly ICosmosDbBuilder _cosmosDbBuilder;

    public IServiceCollection Services { get; }

    public EventStoreBuilder(ICosmosDbBuilder cosmosDbBuilder)
    {
        _cosmosDbBuilder = cosmosDbBuilder;
        Services = cosmosDbBuilder.Services;
    }

    public IEventStoreBuilder AddEventSource<TBaseEvent, TSnapshot>(
        string eventContainerName,
        IReadOnlyCollection<Type>? eventTypes = null,
        JsonSerializerSettings? jsonSerializerSettings = null
    )
        where TBaseEvent : class, IEvent<TSnapshot>
        where TSnapshot : class, ISnapshot, new()
    {
        eventTypes ??= GetEventsOfTypeFromSameAssembly<TBaseEvent>();

        _cosmosDbBuilder.AddContainer<TBaseEvent>(eventContainerName, "/streamId");

        Services.AddSingleton(sp =>
        {
            var cosmosContainer = sp.GetRequiredService<CosmosContainer<TBaseEvent>>();
            var eventStore = new EventStore(
                cosmosContainer.Container,
                new SimpleSerializationTypeMap(eventTypes),
                JsonSerializer.Create(jsonSerializerSettings)
            );
            return new EventStoreDependency<TBaseEvent>(eventStore);
        });

        Services.AddSingleton<EventRepository<TBaseEvent, TSnapshot>>();

        return this;
    }

    public IEventStoreBuilder AddSnapshotStoreProjection<TBaseEvent, TSnapshot>
    (
        string snapshotContainerName,
        string partitionKeyPath = "/partitionKey",
        string? projectorName = null,
        IEnumerable<Collection<CompositePath>>? compositeIndexes = null
    )
        where TBaseEvent : class, ISnapshottableEvent<TSnapshot>
        where TSnapshot : class, ISnapshot, new()
    {
        _cosmosDbBuilder.AddContainer<TSnapshot>(
            snapshotContainerName,
            partitionKeyPath,
            compositeIndexes: compositeIndexes
        );

        Services.AddSingleton<SnapshotRepository<TBaseEvent, TSnapshot>>();

        AddCatchupSubscription<TBaseEvent, SnapshotProjectionCatchupSubscription<TBaseEvent, TSnapshot>>(
            projectorName ?? $"{typeof(TSnapshot).Name}Projector"
        );

        return this;
    }

    public IEventStoreBuilder AddCatchupSubscription<
        TBaseEvent,
        TCatchupSubscriptionHandler
    >(
        string projectorName
    )
        where TCatchupSubscriptionHandler : class, ICatchupSubscription<TBaseEvent>
    {
        Services.AddSingleton<TCatchupSubscriptionHandler>();
        
        _cosmosDbBuilder.AddChangeFeedProcessor<
            CosmosDbStorageEvent,
            TBaseEvent,
            Event<TBaseEvent>,
            EventConverter<TBaseEvent>,
            CatchupSubscriptionChangeFeedProcessor<TBaseEvent, TCatchupSubscriptionHandler>
        >(projectorName);

        return this;
    }

    private static IReadOnlyCollection<Type> GetEventsOfTypeFromSameAssembly<TBaseEvent>()
        where TBaseEvent : class
    {
        return typeof(TBaseEvent)
            .Assembly
            .GetTypes()
            .Where(t => typeof(TBaseEvent).IsAssignableFrom(t))
            .ToList();
    }
}