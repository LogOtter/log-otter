using LogOtter.CosmosDb.EventStore.Metadata;

namespace LogOtter.CosmosDb.EventStore;

public class EventSourceConfiguration<TBaseEvent>
    where TBaseEvent : class
{
    private readonly Type _snapshotProjectionType;
    private readonly Dictionary<Type, IProjectionMetadata<TBaseEvent>> _projections;

    private readonly Dictionary<Type, ICatchUpSubscriptionMetadata> _catchUpSubscriptions;

    internal IReadOnlyCollection<Type> EventTypes { get; private set; }

    internal IReadOnlyCollection<IProjectionMetadata<TBaseEvent>> Projections => _projections.Values;

    internal IReadOnlyCollection<ICatchUpSubscriptionMetadata> CatchUpSubscriptions => _catchUpSubscriptions.Values;

    internal EventSourceConfiguration(Type snapshotProjectionType)
    {
        _snapshotProjectionType = snapshotProjectionType;
        EventTypes = GetEventsOfTypeFromSameAssembly();
        _projections = new Dictionary<Type, IProjectionMetadata<TBaseEvent>>();
        _catchUpSubscriptions = new Dictionary<Type, ICatchUpSubscriptionMetadata>();
    }

    public ProjectionBuilder<TBaseEvent, TProjection> AddProjection<TProjection>()
        where TProjection : class
    {
        var projectionConfiguration = new ProjectionMetadata<TBaseEvent, TProjection>();
        _projections.Add(typeof(TProjection), projectionConfiguration);

        return new ProjectionBuilder<TBaseEvent, TProjection>(_snapshotProjectionType, UpdateMetadata, AddCatchUpSubscription);
    }

    private void AddCatchUpSubscription(ICatchUpSubscriptionMetadata obj)
    {
        _catchUpSubscriptions.Add(obj.HandlerType, obj);
    }

    private void UpdateMetadata<TProjection>(Func<ProjectionMetadata<TBaseEvent, TProjection>, ProjectionMetadata<TBaseEvent, TProjection>> mutate)
    {
        var metaData = (ProjectionMetadata<TBaseEvent, TProjection>)_projections[typeof(TProjection)];
        var newMetadata = mutate(metaData);
        _projections[typeof(TProjection)] = newMetadata;
    }

    public void SpecifyEventTypes(IReadOnlyCollection<Type> events)
    {
        EventTypes = events;
    }

    public void AddCatchupSubscription<TCatchupSubscriptionHandler>(string projectorName)
        where TCatchupSubscriptionHandler : class, ICatchupSubscription<TBaseEvent>
    {
        _catchUpSubscriptions.Add(typeof(TCatchupSubscriptionHandler), new CatchUpSubscriptionMetadata<TCatchupSubscriptionHandler>(projectorName));
    }

    private static IReadOnlyCollection<Type> GetEventsOfTypeFromSameAssembly()
    {
        return typeof(TBaseEvent).Assembly.GetTypes().Where(t => typeof(TBaseEvent).IsAssignableFrom(t)).ToList();
    }
}
