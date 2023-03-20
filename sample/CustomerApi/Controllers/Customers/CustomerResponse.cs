using System.Diagnostics.CodeAnalysis;
using CustomerApi.Events.Customers;

namespace CustomerApi.Controllers.Customers;

public class CustomerResponse
{
    public required string CustomerUri { get; init; }
    public required string EmailAddress { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateTimeOffset CreatedOn { get; init; }

    [SetsRequiredMembers]
    public CustomerResponse(CustomerReadModel customer)
    {
        CustomerUri = customer.CustomerUri.Uri;
        EmailAddress = customer.EmailAddress;
        FirstName = customer.FirstName;
        LastName = customer.LastName;
        CreatedOn = customer.CreatedOn;
    }

    public CustomerResponse() { }
}
