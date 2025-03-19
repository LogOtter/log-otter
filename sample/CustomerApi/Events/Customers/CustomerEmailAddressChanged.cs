using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using LogOtter.Obfuscate;

namespace CustomerApi.Events.Customers;

public class CustomerEmailAddressChanged(CustomerUri customerUri, string oldEmailAddress, string newEmailAddress, DateTimeOffset? timestamp = null)
    : CustomerEvent(customerUri, timestamp)
{
    public string OldEmailAddress { get; } = oldEmailAddress;
    public string NewEmailAddress { get; } = newEmailAddress;

    public override void Apply(CustomerReadModel model, EventInfo eventInfo)
    {
        model.EmailAddress = NewEmailAddress;
    }

    public override string GetDescription()
    {
        return $"Email address changed from '{Obfuscate.Email(OldEmailAddress)}' to '{Obfuscate.Email(NewEmailAddress)}'";
    }
}
