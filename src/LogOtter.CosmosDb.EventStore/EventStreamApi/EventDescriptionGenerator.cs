using System.Collections.Concurrent;
using System.Reflection;
using LogOtter.CosmosDb.EventStore.Metadata;
using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi;

internal class EventDescriptionGenerator
{
    private readonly ConcurrentDictionary<Type, MethodInfo?> _cache;

    public EventDescriptionGenerator()
    {
        _cache = new ConcurrentDictionary<Type, MethodInfo?>();
    }

    public string GetDescription(CosmosDbStorageEvent cosmosDbStorageEvent, IEventSourceMetadata eventSourceMetadata)
    {
        var storageEvent = cosmosDbStorageEvent.ToStorageEvent(
            eventSourceMetadata.SerializationTypeMap,
            JsonSerializer.CreateDefault());

        var methodInfo = GetDescriptionMethod(storageEvent.EventBody);
        if (methodInfo == null)
        {
            return cosmosDbStorageEvent.BodyType;
        }

        try
        {
            var description = (string?)methodInfo.Invoke(storageEvent.EventBody, null);
            return description ?? cosmosDbStorageEvent.BodyType;
        }
        catch
        {
            return cosmosDbStorageEvent.BodyType;
        }
    }

    private MethodInfo? GetDescriptionMethod(object eventBody)
    {
        var eventType = eventBody.GetType();

        return _cache.GetOrAdd(
            eventType,
            type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .SingleOrDefault(m => m.GetCustomAttribute<EventDescriptionAttribute>() != null));
    }
}
