using CustomerApi.Events.Customers;
using CustomerApi.Uris;

namespace CustomerApi.Tests;

public class GivenSteps
{
    private readonly CustomerStore _customerStore;

    public GivenSteps(CustomerStore customerStore)
    {
        _customerStore = customerStore;
    }

    public async Task<CustomerReadModel> AnExistingCustomer(
        CustomerUri customerUri,
        Discretionary<string> emailAddress = default,
        Discretionary<string> firstName = default,
        Discretionary<string> lastName = default
    )
    {
        return await _customerStore.GivenAnExistingCustomer(
            customerUri,
            emailAddress,
            firstName,
            lastName
        );
    }

    public async Task TheCustomerIsDeleted(CustomerUri customerUri)
    {
        await _customerStore.GivenTheCustomerIsDeleted(customerUri);
    }
}