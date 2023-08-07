using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using CustomerApi.Events.Customers;
using CustomerApi.Events.Movies;
using CustomerApi.Uris;

namespace CustomerApi.Tests;

public class GivenSteps
{
    private readonly ConsumerStore _consumerStore;
    private readonly CustomerStore _customerStore;
    private readonly MovieStore _movieStore;

    public GivenSteps(CustomerStore customerStore, ConsumerStore consumerStore, MovieStore movieStore)
    {
        _customerStore = customerStore;
        _consumerStore = consumerStore;
        _movieStore = movieStore;
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
        Discretionary<string> lastName = default
    )
    {
        return await _customerStore.GivenAnExistingCustomer(customerUri, emailAddress, firstName, lastName);
    }

    public async Task<MovieReadModel> AnExistingMovie(MovieUri movieUri, Discretionary<string> name = default)
    {
        return await _movieStore.GivenAnExistingMovie(movieUri, name);
    }

    public async Task TheCustomerIsDeleted(CustomerUri customerUri)
    {
        await _customerStore.GivenTheCustomerIsDeleted(customerUri);
    }
}
