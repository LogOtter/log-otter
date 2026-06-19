using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Customers;

public class CustomerStreamCompactor : IStreamCompactor<CustomerEvent, CustomerReadModel>
{
    public const string RedactedValue = "[REDACTED]";

    public CustomerEvent CreateTombstoneEvent(CustomerReadModel currentProjection, string streamId)
    {
        return new CustomerCompacted(
            currentProjection.CustomerUri,
            RedactedValue,
            RedactedValue,
            RedactedValue,
            currentProjection.CreatedOn
        );
    }
}
