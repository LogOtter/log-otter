using LogOtter.CosmosDb.EventStore.Metadata;
using LogOtter.CosmosDb.Metadata;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.EventStore;

public class SnapshotBuilder<TBaseEvent, TProjection>
{
    private readonly Action<Func<ProjectionMetadata<TBaseEvent, TProjection>, ProjectionMetadata<TBaseEvent, TProjection>>> _mutateMetadata;

    internal SnapshotBuilder(Action<Func<ProjectionMetadata<TBaseEvent, TProjection>, ProjectionMetadata<TBaseEvent, TProjection>>> mutateMetadata)
    {
        _mutateMetadata = mutateMetadata;
    }

    public SnapshotBuilder<TBaseEvent, TProjection> WithAutoProvisionSettings(
        string partitionKeyPath = "/partitionKey",
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = -1,
        IndexingPolicy? indexingPolicy = null,
        ThroughputProperties? throughputProperties = null
    )
    {
        _mutateMetadata(md =>
            md with
            {
                SnapshotMetadata = md.SnapshotMetadata! with
                {
                    AutoProvisionMetadata = new AutoProvisionMetadata(
                        partitionKeyPath,
                        uniqueKeyPolicy,
                        defaultTimeToLive,
                        indexingPolicy,
                        throughputProperties
                    ),
                },
            }
        );
        return this;
    }
}
