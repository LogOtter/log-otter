using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Newtonsoft.Json;

namespace CustomerApi.Events.Customers;

public abstract class CustomerEvent : ISnapshottableEvent<CustomerReadModel>
{
    public string SnapshotPartitionKey => CustomerReadModel.StaticPartitionKey;
    
    public string EventStreamId => CustomerUri.Uri;

    [JsonProperty("ttl")]
    public int? Ttl => -1;
    
    public CustomerUri CustomerUri { get; }

    public DateTimeOffset Timestamp { get; }

    public CustomerEvent(CustomerUri customerUri, DateTimeOffset? timestamp = null)
    {
        CustomerUri = customerUri;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }

    public abstract void Apply(CustomerReadModel model);
}