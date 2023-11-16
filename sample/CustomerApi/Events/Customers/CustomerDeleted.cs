using CustomerApi.Uris;

namespace CustomerApi.Events.Customers;

public class CustomerDeleted(CustomerUri customerUri, DateTimeOffset? timestamp = null) : CustomerEvent(customerUri, timestamp)
{
    public override void Apply(CustomerReadModel model)
    {
        model.DeletedAt = Timestamp;
    }

    public override string GetDescription()
    {
        return "Deleted";
    }
}
