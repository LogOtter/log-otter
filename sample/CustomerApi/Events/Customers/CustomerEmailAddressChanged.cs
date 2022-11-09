using CustomerApi.Uris;

namespace CustomerApi.Events.Customers;

public class CustomerEmailAddressChanged : CustomerEvent
{
    public string OldEmailAddress { get; }
    public string NewEmailAddress { get; }

    public CustomerEmailAddressChanged(
        CustomerUri customerUri,
        string oldEmailAddress,
        string newEmailAddress,
        DateTimeOffset? timestamp = null
    ) : base(customerUri, timestamp)
    {
        OldEmailAddress = oldEmailAddress;
        NewEmailAddress = newEmailAddress;
    }

    public override void Apply(CustomerReadModel model)
    {
        model.EmailAddress = NewEmailAddress;
    }
}