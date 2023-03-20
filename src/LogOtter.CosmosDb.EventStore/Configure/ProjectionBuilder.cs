using LogOtter.CosmosDb.EventStore.Metadata;

namespace LogOtter.CosmosDb.EventStore;

public class ProjectionBuilder<TBaseEvent, TProjection>
{
    private readonly Action<Func<ProjectionMetadata<TBaseEvent, TProjection>, ProjectionMetadata<TBaseEvent, TProjection>>> _mutateMetadata;

    private readonly Action<ICatchUpSubscriptionMetadata> _addCatchUpSubscription;

    internal ProjectionBuilder(Action<Func<ProjectionMetadata<TBaseEvent, TProjection>, ProjectionMetadata<TBaseEvent, TProjection>>> mutateMetadata, Action<ICatchUpSubscriptionMetadata> addCatchUpSubscription)
    {
        _mutateMetadata = mutateMetadata;
        _addCatchUpSubscription = addCatchUpSubscription;
    }

    public SnapshotBuilder<TBaseEvent, TProjection> WithSnapshot(string containerName, Func<TBaseEvent, string> getSnapshotPartitionFromEvent, string partitionKeyPath = "/partitionKey")
    {
        _mutateMetadata(md => md with {SnapshotMetadata = new(containerName, partitionKeyPath, getSnapshotPartitionFromEvent)});

        var snapshotProjectionCatchupSubscription = typeof(SnapshotProjectionCatchupSubscription<,>);
        var specificHandlerType = snapshotProjectionCatchupSubscription.MakeGenericType(typeof(TBaseEvent), typeof(TProjection));
        var catchUpSubscriptionMetadata = typeof(CatchUpSubscriptionMetadata<>);
        var specificMetadata = catchUpSubscriptionMetadata.MakeGenericType(specificHandlerType);
        var projector = Activator.CreateInstance(specificMetadata, containerName + "ProjectionSnapshotPersister") ?? throw new InvalidOperationException("Unable to create metadata for snapshot");
        _addCatchUpSubscription((ICatchUpSubscriptionMetadata)projector);

        return new SnapshotBuilder<TBaseEvent, TProjection>(_mutateMetadata);
    }
}
