using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using LogOtter.Obfuscate;

namespace CustomerApi.Events.Customers;

public class CustomerNameChanged(
    CustomerUri customerUri,
    string oldFirstName,
    string newFirstName,
    string oldLastName,
    string newLastName,
    DateTimeOffset? timestamp = null
) : CustomerEvent(customerUri, timestamp)
{
    public string OldFirstName { get; } = oldFirstName;
    public string OldLastName { get; } = oldLastName;
    public string NewFirstName { get; } = newFirstName;
    public string NewLastName { get; } = newLastName;

    public override void Apply(CustomerReadModel model, EventInfo eventInfo)
    {
        model.FirstName = NewFirstName;
        model.LastName = NewLastName;
    }

    public override string GetDescription()
    {
        return $"Name changed from '{Obfuscate.Name(OldFirstName, OldLastName)}' to '{Obfuscate.Name(NewFirstName, NewLastName)}'";
    }
}
