using System.Collections.Immutable;
using LogOtter.CosmosDb.EventStore.Metadata;
using LogOtter.CosmosDb.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.EventStore;

public class EventSourcingBuilder
{
    private readonly CosmosDbBuilder _cosmosDbBuilder;

    public IServiceCollection Services { get; }

    internal EventSourcingBuilder(CosmosDbBuilder cosmosDbBuilder)
    {
        _cosmosDbBuilder = cosmosDbBuilder;
        Services = cosmosDbBuilder.Services;
    }

    public EventSourcingBuilder AddEventSource<TBaseEvent>(string containerName, Action<EventSourceConfiguration<TBaseEvent>>? configure = null)
        where TBaseEvent : class
    {
        var config = new EventSourceConfiguration<TBaseEvent>();
        configure?.Invoke(config);

        _cosmosDbBuilder.AddContainer(typeof(TBaseEvent), containerName, new AutoProvisionMetadata(PartitionKeyPath: "/streamId"));

        var simpleSerializationTypeMap = new SimpleSerializationTypeMap(config.EventTypes);
        var metadata = new EventSourceMetadata<TBaseEvent>(
            containerName,
            config.EventTypes.ToImmutableList(),
            simpleSerializationTypeMap,
            config.Projections.Cast<IProjectionMetadata>().ToImmutableList(),
            ImmutableList<ICatchUpSubscriptionMetadata>.Empty
        );

        Services.AddSingleton(metadata);
        Services.AddSingleton<IEventSourceMetadata>(metadata);
        _cosmosDbBuilder.WithCustomJsonConverter<StorageEventJsonConverter<TBaseEvent>>(
            sp => new StorageEventJsonConverter<TBaseEvent>(metadata.SerializationTypeMap)
        );

        Services.AddSingleton(sp =>
        {
            var cosmosContainer = sp.GetRequiredService<CosmosContainer<TBaseEvent>>();
            var feedIteratorFactory = sp.GetRequiredService<IFeedIteratorFactory>();

            return new EventStore<TBaseEvent>(cosmosContainer.Container, feedIteratorFactory, simpleSerializationTypeMap);
        });

        var eventRepository = typeof(EventRepository<,>);
        var snapshotRepository = typeof(SnapshotRepository<,>);
        foreach (var projection in config.Projections)
        {
            Services.AddSingleton(eventRepository.MakeGenericType(typeof(TBaseEvent), projection.ProjectionType));

            if (projection.SnapshotMetadata != null)
            {
                _cosmosDbBuilder.AddContainer(
                    projection.ProjectionType,
                    projection.SnapshotMetadata.ContainerName,
                    projection.SnapshotMetadata.AutoProvisionMetadata
                );

                Services.AddSingleton(snapshotRepository.MakeGenericType(typeof(TBaseEvent), projection.ProjectionType));
            }
        }

        var catchupSubscriptionChangeFeedProcessor = typeof(CatchupSubscriptionChangeFeedProcessor<,>);
        foreach (var catchUpSubscription in config.CatchUpSubscriptions)
        {
            Services.AddSingleton(catchUpSubscription.HandlerType);

            var specificCatchUpSubscriptionType = catchupSubscriptionChangeFeedProcessor.MakeGenericType(
                typeof(TBaseEvent),
                catchUpSubscription.HandlerType
            );

            _cosmosDbBuilder.AddChangeFeedProcessor(
                typeof(StorageEvent<TBaseEvent>),
                typeof(TBaseEvent),
                typeof(Event<TBaseEvent>),
                typeof(EventConverter<TBaseEvent>),
                specificCatchUpSubscriptionType,
                catchUpSubscription.ProjectorName
            );

            return this;
        }

        return this;
    }
}
