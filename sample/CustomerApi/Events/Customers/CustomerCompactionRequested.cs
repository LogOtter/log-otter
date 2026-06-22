using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Customers;

public class CustomerCompactionRequested(CustomerUri customerUri, DateTimeOffset? timestamp = null)
    : CustomerEvent(customerUri, timestamp),
        ICompactionRequestedEvent<CustomerReadModel>
{
    public override void Apply(CustomerReadModel model, EventInfo eventInfo)
    {
        // No projection change — this event exists solely as a signal to the compaction change feed processor.
    }

    public override string GetDescription()
    {
        return "Compaction requested";
    }
}
