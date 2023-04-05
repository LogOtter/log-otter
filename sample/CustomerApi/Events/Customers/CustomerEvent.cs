using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Newtonsoft.Json;

namespace CustomerApi.Events.Customers;

public abstract class CustomerEvent : IEvent<CustomerReadModel>
{
    public CustomerUri CustomerUri { get; }

    public DateTimeOffset Timestamp { get; }

    public CustomerEvent(CustomerUri customerUri, DateTimeOffset? timestamp = null)
    {
        CustomerUri = customerUri;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }

    public string EventStreamId => CustomerUri.Uri;

    public abstract void Apply(CustomerReadModel model);

    [EventDescription]
    public abstract string? GetDescription();
}
