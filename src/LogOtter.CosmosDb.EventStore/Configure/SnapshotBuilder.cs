using System.Collections.ObjectModel;
using LogOtter.CosmosDb.EventStore.Metadata;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.EventStore;

public class SnapshotBuilder<TBaseEvent, TProjection>
{
    private readonly Action<Func<ProjectionMetadata<TBaseEvent, TProjection>, ProjectionMetadata<TBaseEvent, TProjection>>> _mutateMetadata;

    internal SnapshotBuilder(Action<Func<ProjectionMetadata<TBaseEvent, TProjection>, ProjectionMetadata<TBaseEvent, TProjection>>> mutateMetadata)
    {
        _mutateMetadata = mutateMetadata;
    }

    public SnapshotBuilder<TBaseEvent, TProjection> AutoProvision(IReadOnlyCollection<Collection<CompositePath>> compositeIndexes)
    {
        _mutateMetadata(
            md => md with { SnapshotMetadata = md.SnapshotMetadata! with { AutoProvisionMetadata = new AutoProvisionMetadata(compositeIndexes) } }
        );
        return this;
    }
}
