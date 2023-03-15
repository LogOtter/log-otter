using System.Linq.Expressions;
using CustomerApi.Events.Customers;
using CustomerApi.Uris;

namespace CustomerApi.Tests;

public class ThenSteps
{
    private readonly CustomerStore _customerStore;

    public ThenSteps(CustomerStore customerStore)
    {
        _customerStore = customerStore;
    }

    public async Task TheCustomerShouldBeDeleted(CustomerUri customerUri)
    {
        await _customerStore.ThenTheCustomerShouldBeDeleted(customerUri);
    }

    public async Task TheCustomerShouldMatch(CustomerUri customerUri, Expression<Func<CustomerReadModel, bool>> matchFunc)
    {
        await _customerStore.ThenTheCustomerShouldMatch(customerUri, matchFunc);
    }
}
