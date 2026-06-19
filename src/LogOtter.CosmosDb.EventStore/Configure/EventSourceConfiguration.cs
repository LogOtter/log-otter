using LogOtter.CosmosDb.EventStore.Metadata;

namespace LogOtter.CosmosDb.EventStore;

public class EventSourceConfiguration<TBaseEvent>
    where TBaseEvent : class
{
    private readonly Dictionary<Type, IProjectionMetadata<TBaseEvent>> _projections;

    private readonly Dictionary<Type, ICatchUpSubscriptionMetadata> _catchUpSubscriptions;

    private readonly List<CompactionMetadata> _compactions;

    internal IReadOnlyCollection<Type> EventTypes { get; private set; }

    internal IReadOnlyCollection<IProjectionMetadata<TBaseEvent>> Projections => _projections.Values;

    internal IReadOnlyCollection<ICatchUpSubscriptionMetadata> CatchUpSubscriptions => _catchUpSubscriptions.Values;

    internal IReadOnlyCollection<CompactionMetadata> Compactions => _compactions;

    internal Func<IServiceProvider, Task<bool>>? EnabledFunc;

    internal EventSourceConfiguration()
    {
        EventTypes = GetEventsOfTypeFromSameAssembly();
        _projections = new Dictionary<Type, IProjectionMetadata<TBaseEvent>>();
        _catchUpSubscriptions = new Dictionary<Type, ICatchUpSubscriptionMetadata>();
        _compactions = new List<CompactionMetadata>();
    }

    public void WithCompaction<TCompactor, TSnapshot>()
        where TCompactor : class
        where TSnapshot : class, ISnapshot, new()
    {
        var compactorInterface = typeof(IStreamCompactor<,>).MakeGenericType(typeof(TBaseEvent), typeof(TSnapshot));
        if (!compactorInterface.IsAssignableFrom(typeof(TCompactor)))
        {
            throw new ArgumentException(
                $"{typeof(TCompactor).Name} must implement {compactorInterface.Name}",
                nameof(TCompactor)
            );
        }

        _compactions.Add(new CompactionMetadata(typeof(TCompactor), typeof(TSnapshot)));
    }

    public ProjectionBuilder<TBaseEvent, TProjection> AddProjection<TProjection>()
        where TProjection : class
    {
        var projectionConfiguration = new ProjectionMetadata<TBaseEvent, TProjection>();
        _projections.Add(typeof(TProjection), projectionConfiguration);
        return new ProjectionBuilder<TBaseEvent, TProjection>(UpdateMetadata, AddCatchUpSubscription);
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

    public void SpecifyEnabledFunc(Func<IServiceProvider, Task<bool>> enabledFunc)
    {
        EnabledFunc = enabledFunc;
    }

    private static IReadOnlyCollection<Type> GetEventsOfTypeFromSameAssembly()
    {
        return typeof(TBaseEvent).Assembly.GetTypes().Where(t => typeof(TBaseEvent).IsAssignableFrom(t)).ToList();
    }
}
