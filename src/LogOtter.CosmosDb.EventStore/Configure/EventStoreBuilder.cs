using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore;

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

        _cosmosDbBuilder.AddContainer<TBaseEvent>(
            eventContainerName,
            "/streamId",
            defaultTimeToLive: -1
        );

        var metadata = new EventStoreMetadata(
            typeof(TBaseEvent),
            typeof(TSnapshot),
            eventContainerName,
            eventTypes,
            new SimpleSerializationTypeMap(eventTypes),
            jsonSerializerSettings
        );
        
        Services.AddSingleton(metadata);

        Services.AddSingleton(sp =>
        {
            var cosmosContainer = sp.GetRequiredService<CosmosContainer<TBaseEvent>>();
            var feedIteratorFactory = sp.GetRequiredService<IFeedIteratorFactory>();
            var eventStore = new EventStore(
                cosmosContainer.Container,
                feedIteratorFactory,
                metadata.SerializationTypeMap,
                JsonSerializer.Create(metadata.JsonSerializerSettings)
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
            compositeIndexes: compositeIndexes,
            defaultTimeToLive: -1
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