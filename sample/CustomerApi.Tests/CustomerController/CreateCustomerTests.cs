using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Customers.Create;
using CustomerApi.Uris;
using FluentAssertions;
using Xunit;

namespace CustomerApi.Tests.CustomerController;

public class CreateCustomerTests
{
    [Fact]
    public async Task Valid_ReturnsOk()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Valid_StoredCorrectly()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);
        var location = response.Headers.Location?.ToString();
        location.Should().NotBeNull("Location should be returned");

        var customerUri = CustomerUri.Parse(location!);

        await customerApi.Then.TheCustomerShouldMatch(
            customerUri,
            c => c.EmailAddress == "bob@bobertson.co.uk" && c.FirstName == "Bob" && c.LastName == "Bobertson");
    }

    [Theory]
    [InlineData(null, "Bob", "Bobertson")]
    [InlineData("", "Bob", "Bobertson")]
    [InlineData("bob@bobertson.co.uk", null, "Bobertson")]
    [InlineData("bob@bobertson.co.uk", "", "Bobertson")]
    [InlineData("bob@bobertson.co.uk", "Bob", null)]
    [InlineData("bob@bobertson.co.uk", "Bob", "")]
    [InlineData("not-an-email-address", "Bob", "Bobertson")]
    public async Task Invalid_ReturnsBadRequest(string email, string firstName, string lastName)
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateCustomerRequest(email, firstName, lastName);

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthorized()
    {
        using var customerApi = new TestCustomerApi();

        var client = customerApi.CreateClient();
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forbidden()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.InvalidRole");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
