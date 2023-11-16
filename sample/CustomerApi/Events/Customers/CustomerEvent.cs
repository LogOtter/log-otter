using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Newtonsoft.Json;

namespace CustomerApi.Events.Customers;

public abstract class CustomerEvent(CustomerUri customerUri, DateTimeOffset? timestamp = null) : IEvent<CustomerReadModel>
{
    public CustomerUri CustomerUri { get; } = customerUri;

    public DateTimeOffset Timestamp { get; } = timestamp ?? DateTimeOffset.UtcNow;

    public string EventStreamId => CustomerUri.Uri;

    public abstract void Apply(CustomerReadModel model);

    [EventDescription]
    public abstract string? GetDescription();
}
