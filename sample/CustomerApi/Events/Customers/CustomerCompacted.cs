using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Customers;

public class CustomerCompacted(
    CustomerUri customerUri,
    string emailAddress,
    string firstName,
    string lastName,
    DateTimeOffset createdOn,
    DateTimeOffset? timestamp = null
) : CustomerEvent(customerUri, timestamp), ICompactionEvent
{
    public string EmailAddress { get; } = emailAddress;

    public string FirstName { get; } = firstName;

    public string LastName { get; } = lastName;

    public DateTimeOffset CreatedOn { get; } = createdOn;

    public override void Apply(CustomerReadModel model, EventInfo eventInfo)
    {
        model.CustomerUri = CustomerUri;
        model.EmailAddress = EmailAddress;
        model.EmailAddresses = new List<string>();
        model.FirstName = FirstName;
        model.LastName = LastName;
        model.CreatedOn = CreatedOn;
    }

    public override string GetDescription()
    {
        return "Compacted (PII scrubbed)";
    }
}
