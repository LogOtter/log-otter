using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace LogOtter.EventStore.CosmosDb;

public interface IEventStoreBuilder
{
    IEventStoreBuilder AddEventSource<TBaseEvent, TSnapshot>(
        string eventContainerName,
        IReadOnlyCollection<Type>? eventTypes = null,
        JsonSerializerSettings? jsonSerializerSettings = null
    )
        where TBaseEvent : class, IEvent<TSnapshot>
        where TSnapshot : class, ISnapshot, new();

    IEventStoreBuilder AddSnapshotStoreProjection<TBaseEvent, TSnapshot>
    (
        string snapshotContainerName,
        string partitionKeyPath = "/partitionKey",
        string? projectorName = null,
        IEnumerable<Collection<CompositePath>>? compositeIndexes = null
    )
        where TBaseEvent : class, ISnapshottableEvent<TSnapshot>
        where TSnapshot : class, ISnapshot, new();

    IEventStoreBuilder AddCatchupSubscription<
        TBaseEvent,
        TCatchupSubscriptionHandler
    >(
        string projectorName
    ) where TCatchupSubscriptionHandler : class, ICatchupSubscription<TBaseEvent>;
}