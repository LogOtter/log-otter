using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Customers;
using CustomerApi.Uris;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CustomerApi.Tests.CustomerController;

public class GetCustomerByIdTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Valid_ReturnsOk()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"), "bob@bobertson.co.uk", "Bob", "Bobertson");

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers/ExistingUser");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();

        customer.ShouldNotBeNull();
        customer!.EmailAddress.ShouldBe("bob@bobertson.co.uk");
        customer.FirstName.ShouldBe("Bob");
        customer.LastName.ShouldBe("Bobertson");
        customer.CustomerUri.ShouldBe("/customers/ExistingUser");
        customer.CreatedOn.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task NotFound()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers/NonExistentUser");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unauthorized()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"));

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/customers/ExistingUser");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forbidden()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"));
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.InvalidRole");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers/ExistingUser");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
