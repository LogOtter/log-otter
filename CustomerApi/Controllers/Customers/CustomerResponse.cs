using CustomerApi.Events.Customers;

namespace CustomerApi.Controllers.Customers;

public class CustomerResponse
{
    public string CustomerUri { get; set; }
    public string EmailAddress { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    public CustomerResponse(CustomerReadModel customer)
    {
        CustomerUri = customer.CustomerUri.Uri;
        EmailAddress = customer.EmailAddress;
        FirstName = customer.FirstName;
        LastName = customer.LastName;
        CreatedOn = customer.CreatedOn;
    }
}