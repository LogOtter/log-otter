using CustomerApi.Uris;
using LogOtter.Obfuscate;

namespace CustomerApi.Events.Customers;

public class CustomerNameChanged : CustomerEvent
{
    public string OldFirstName { get; }
    public string OldLastName { get; }
    public string NewFirstName { get; }
    public string NewLastName { get; }

    public CustomerNameChanged(
        CustomerUri customerUri,
        string oldFirstName,
        string newFirstName,
        string oldLastName,
        string newLastName,
        DateTimeOffset? timestamp = null
    )
        : base(customerUri, timestamp)
    {
        OldFirstName = oldFirstName;
        NewFirstName = newFirstName;
        OldLastName = oldLastName;
        NewLastName = newLastName;
    }

    public override void Apply(CustomerReadModel model)
    {
        model.FirstName = NewFirstName;
        model.LastName = NewLastName;
    }

    public override string GetDescription()
    {
        return $"Name changed from '{Obfuscate.Name(OldFirstName, OldLastName)}' to '{Obfuscate.Name(NewFirstName, NewLastName)}'";
    }
}
