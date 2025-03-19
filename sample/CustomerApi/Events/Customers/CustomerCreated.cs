using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using LogOtter.Obfuscate;

namespace CustomerApi.Events.Customers;

public class CustomerCreated(CustomerUri customerUri, string emailAddress, string firstName, string lastName, DateTimeOffset? timestamp = null)
    : CustomerEvent(customerUri, timestamp)
{
    public string EmailAddress { get; } = emailAddress;

    public string FirstName { get; } = firstName;

    public string LastName { get; } = lastName;

    public override void Apply(CustomerReadModel model, EventInfo eventInfo)
    {
        model.CustomerUri = CustomerUri;
        model.EmailAddress = EmailAddress;
        model.FirstName = FirstName;
        model.LastName = LastName;
        model.CreatedOn = Timestamp;
    }

    public override string GetDescription()
    {
        return $"{Obfuscate.Email(EmailAddress)} created";
    }
}
