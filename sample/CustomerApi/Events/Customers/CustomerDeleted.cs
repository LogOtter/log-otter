using CustomerApi.Uris;

namespace CustomerApi.Events.Customers;

public class CustomerDeleted : CustomerEvent
{
    public CustomerDeleted(CustomerUri customerUri, DateTimeOffset? timestamp = null)
        : base(customerUri, timestamp)
    {
    }

    public override void Apply(CustomerReadModel model)
    {
        model.DeletedAt = Timestamp;
    }

    public override string GetDescription()
    {
        return "Deleted";
    }
}