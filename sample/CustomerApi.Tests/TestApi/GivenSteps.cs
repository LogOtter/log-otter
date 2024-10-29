using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using CustomerApi.Events.Customers;
using CustomerApi.Events.Movies;
using CustomerApi.Uris;

namespace CustomerApi.Tests;

public class GivenSteps(CustomerStore customerStore, ConsumerStore consumerStore, MovieStore movieStore)
{
    public Task<AuthenticationHeaderValue> AnExistingConsumer(params string[] roles)
    {
        var token = new JwtSecurityTokenHandler().WriteToken(consumerStore.GivenAnExistingConsumer("customer-api", roles));

        return Task.FromResult(new AuthenticationHeaderValue("Bearer", token));
    }

    public async Task<CustomerReadModel> AnExistingCustomer(
        CustomerUri customerUri,
        Discretionary<string> emailAddress = default,
        Discretionary<string> firstName = default,
        Discretionary<string> lastName = default
    )
    {
        return await customerStore.GivenAnExistingCustomer(customerUri, emailAddress, firstName, lastName);
    }

    public async Task<MovieReadModel> AnExistingMovie(MovieUri movieUri, Discretionary<string> name = default)
    {
        return await movieStore.GivenAnExistingMovie(movieUri, name);
    }

    public async Task<MovieReadModel> AnExistingMovieWithAProjectedSnapshot(MovieUri movieUri, Discretionary<string> name = default)
    {
        return await movieStore.GivenAnExistingMovieSnapshot(movieUri, name);
    }

    public async Task<MovieReadModel> AnExistingMovieNameIsChangedButNotProjected(MovieUri movieUri, string newName)
    {
        return await movieStore.GivenAnExistingMovieNameIsChanged(movieUri, newName);
    }

    public async Task TheCustomerIsDeleted(CustomerUri customerUri)
    {
        await customerStore.GivenTheCustomerIsDeleted(customerUri);
    }

    public void CreatingACustomerWillConflict()
    {
        customerStore.GivenCreatingACustomerWillConflict();
    }
}
