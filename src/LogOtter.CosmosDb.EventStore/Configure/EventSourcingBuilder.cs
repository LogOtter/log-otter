using System.Collections.Immutable;
using LogOtter.CosmosDb.EventStore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.EventStore;

public class EventSourcingBuilder
{
    private readonly CosmosDbBuilder _cosmosDbBuilder;

    public IServiceCollection Services { get; }

    public EventSourcingBuilder(CosmosDbBuilder cosmosDbBuilder)
    {
        _cosmosDbBuilder = cosmosDbBuilder;
        Services = cosmosDbBuilder.Services;
    }

    public EventSourcingBuilder AddEventSource<TBaseEvent>(string containerName, Action<EventSourceConfiguration<TBaseEvent>>? configure = null)
    {
        var config = new EventSourceConfiguration<TBaseEvent>();
        configure?.Invoke(config);

        _cosmosDbBuilder.AddContainer<TBaseEvent>(containerName, "/streamId", defaultTimeToLive: -1);

        var metadata = new EventSourceMetadata<TBaseEvent>(
            containerName,
            config.EventTypes.ToImmutableList(),
            new SimpleSerializationTypeMap(config.EventTypes),
            config.Projections.Cast<IProjectionMetadata>().ToImmutableList(),
            ImmutableList<ICatchUpSubscriptionMetadata>.Empty);

        Services.AddSingleton(metadata);
        Services.AddSingleton<IEventSourceMetadata>(metadata);

        Services.AddSingleton(
            sp =>
            {
                var cosmosContainer = sp.GetRequiredService<CosmosContainer<TBaseEvent>>();
                var feedIteratorFactory = sp.GetRequiredService<IFeedIteratorFactory>();
                var eventStore = new EventStore(
                    cosmosContainer.Container,
                    feedIteratorFactory,
                    metadata.SerializationTypeMap);
                return new EventStoreDependency<TBaseEvent>(eventStore);
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
                    projection.SnapshotMetadata.PartitionKeyPath,
                    compositeIndexes: projection.SnapshotMetadata.AutoProvisionMetadata?.CompositeIndexes,
                    defaultTimeToLive: -1);

                Services.AddSingleton(snapshotRepository.MakeGenericType(typeof(TBaseEvent), projection.ProjectionType));
            }
        }

        var catchupSubscriptionChangeFeedProcessor = typeof(CatchupSubscriptionChangeFeedProcessor<,>);
        foreach (var catchUpSubscription in config.CatchUpSubscriptions)
        {
            Services.AddSingleton(catchUpSubscription.HandlerType);

            var specificCatchUpSubscriptionType =
                catchupSubscriptionChangeFeedProcessor.MakeGenericType(typeof(TBaseEvent), catchUpSubscription.HandlerType);

            _cosmosDbBuilder.AddChangeFeedProcessor(
                typeof(CosmosDbStorageEvent),
                typeof(TBaseEvent),
                typeof(Event<TBaseEvent>),
                typeof(EventConverter<TBaseEvent>),
                specificCatchUpSubscriptionType,
                catchUpSubscription.ProjectorName);

            return this;
        }

        return this;
    }
}
