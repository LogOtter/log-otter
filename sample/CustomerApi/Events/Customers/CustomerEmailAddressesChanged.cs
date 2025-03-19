using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using LogOtter.Obfuscate;

namespace CustomerApi.Events.Customers;

public class CustomerEmailAddressesChanged(
    CustomerUri customerUri,
    List<string> oldEmailAddresses,
    List<string> newEmailAddresses,
    DateTimeOffset? timestamp = null
) : CustomerEvent(customerUri, timestamp)
{
    public List<string> OldEmailAddresses { get; } = oldEmailAddresses;
    public List<string> NewEmailAddresses { get; } = newEmailAddresses;

    public override void Apply(CustomerReadModel model, EventInfo eventInfo)
    {
        model.EmailAddresses = NewEmailAddresses;
    }

    public override string GetDescription()
    {
        return $"Email address changed from '{
            string.Join("; ",OldEmailAddresses.Select(Obfuscate.Email))}' to '{string.Join("; ",NewEmailAddresses.Select(Obfuscate.Email))}'";
    }
}
