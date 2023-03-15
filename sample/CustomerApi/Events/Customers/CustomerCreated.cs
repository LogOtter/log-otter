using CustomerApi.Uris;
using LogOtter.Obfuscate;

namespace CustomerApi.Events.Customers;

public class CustomerCreated : CustomerEvent
{
    public string EmailAddress { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public CustomerCreated(CustomerUri customerUri, string emailAddress, string firstName, string lastName, DateTimeOffset? timestamp = null) : base(
        customerUri,
        timestamp)
    {
        EmailAddress = emailAddress;
        FirstName = firstName;
        LastName = lastName;
    }

    public override void Apply(CustomerReadModel model)
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
