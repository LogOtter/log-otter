namespace LogOtter.CosmosDb.EventStore.Metadata;

public class SnapshotPartitionKeyResolverFactory
{
    private readonly IDictionary<(Type BaseEventType, Type ProjectType), ISnapshotMetadata> _projections;

    internal SnapshotPartitionKeyResolverFactory(IEnumerable<IEventSourceMetadata> eventSourceMetadata)
    {
        _projections = eventSourceMetadata.SelectMany(e => e.Projections)
                                          .Where(p => p.SnapshotMetadata != null)
                                          .ToDictionary(p => (p.EventType, p.ProjectionType), p => p.SnapshotMetadata!);
    }

    public Func<TBaseEvent, string> GetResolver<TBaseEvent, TProjection>()
    {
        if (!_projections.TryGetValue((typeof(TBaseEvent), typeof(TProjection)), out var snapshotMetadata))
        {
            throw new ArgumentException($"No snapshot registered for {typeof(TBaseEvent).Name} => {typeof(TProjection).Name}");
        }

        var typedSnapshotMetadata = (SnapshotMetadata<TBaseEvent, TProjection>)snapshotMetadata;

        return typedSnapshotMetadata.SnapshotPartitionKeyResolver;
    }
}
