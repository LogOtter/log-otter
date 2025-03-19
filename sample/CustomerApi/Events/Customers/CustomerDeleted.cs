using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Customers;

public class CustomerDeleted(CustomerUri customerUri, DateTimeOffset? timestamp = null) : CustomerEvent(customerUri, timestamp)
{
    public override void Apply(CustomerReadModel model, EventInfo eventInfo)
    {
        model.DeletedAt = Timestamp;
    }

    public override string GetDescription()
    {
        return "Deleted";
    }
}
