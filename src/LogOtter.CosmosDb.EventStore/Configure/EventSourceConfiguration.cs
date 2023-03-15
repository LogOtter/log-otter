﻿using LogOtter.CosmosDb.EventStore.Metadata;

namespace LogOtter.CosmosDb.EventStore;

public class EventSourceConfiguration<TBaseEvent>
{
    private readonly Dictionary<Type, IProjectionMetadata<TBaseEvent>> _projections;

    private readonly Dictionary<Type, ICatchUpSubscriptionMetadata> _catchUpSubscriptions;

    public IReadOnlyCollection<Type> EventTypes { get; private set; }

    public IReadOnlyCollection<IProjectionMetadata<TBaseEvent>> Projections => _projections.Values;

    public IReadOnlyCollection<ICatchUpSubscriptionMetadata> CatchUpSubscriptions => _catchUpSubscriptions.Values;

    public EventSourceConfiguration()
    {
        EventTypes = GetEventsOfTypeFromSameAssembly();
        _projections = new Dictionary<Type, IProjectionMetadata<TBaseEvent>>();
        _catchUpSubscriptions = new Dictionary<Type, ICatchUpSubscriptionMetadata>();
    }

    public ProjectionBuilder<TBaseEvent, TProjection> AddProjection<TProjection>()
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

    private static IReadOnlyCollection<Type> GetEventsOfTypeFromSameAssembly()
    {
        return typeof(TBaseEvent).Assembly.GetTypes().Where(t => typeof(TBaseEvent).IsAssignableFrom(t)).ToList();
    }
}