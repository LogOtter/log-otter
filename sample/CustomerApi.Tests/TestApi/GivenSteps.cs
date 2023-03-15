using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using CustomerApi.Events.Customers;
using CustomerApi.Uris;

namespace CustomerApi.Tests;

public class GivenSteps
{
    private readonly ConsumerStore _consumerStore;
    private readonly CustomerStore _customerStore;

    public GivenSteps(CustomerStore customerStore, ConsumerStore consumerStore)
    {
        _customerStore = customerStore;
        _consumerStore = consumerStore;
    }

    public Task<AuthenticationHeaderValue> AnExistingConsumer(params string[] roles)
    {
        var token = new JwtSecurityTokenHandler().WriteToken(_consumerStore.GivenAnExistingConsumer("customer-api", roles));

        return Task.FromResult(new AuthenticationHeaderValue("Bearer", token));
    }

    public async Task<CustomerReadModel> AnExistingCustomer(
        CustomerUri customerUri,
        Discretionary<string> emailAddress = default,
        Discretionary<string> firstName = default,
        Discretionary<string> lastName = default)
    {
        return await _customerStore.GivenAnExistingCustomer(
            customerUri,
            emailAddress,
            firstName,
            lastName);
    }

    public async Task TheCustomerIsDeleted(CustomerUri customerUri)
    {
        await _customerStore.GivenTheCustomerIsDeleted(customerUri);
    }
}
