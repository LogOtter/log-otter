using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Customers;
using CustomerApi.Uris;
using FluentAssertions;
using Xunit;

namespace CustomerApi.Tests.CustomerController;

public class GetCustomerByIdTests
{
    [Fact]
    public async Task Valid_ReturnsOk()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"), "bob@bobertson.co.uk", "Bob", "Bobertson");

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers/ExistingUser");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();

        customer.Should().NotBeNull();
        customer!.EmailAddress.Should().Be("bob@bobertson.co.uk");
        customer.FirstName.Should().Be("Bob");
        customer.LastName.Should().Be("Bobertson");
        customer.CustomerUri.Should().Be("/customers/ExistingUser");
        customer.CreatedOn.Should().BeWithin(TimeSpan.FromSeconds(1)).Before(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task NotFound()
    {
        using var customerApi = new TestCustomerApi();

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers/NonExistentUser");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unauthorized()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"));

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/customers/ExistingUser");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forbidden()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"));
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.InvalidRole");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers/ExistingUser");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
