using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Customers;
using CustomerApi.Uris;
using FluentAssertions;
using Xunit;

namespace CustomerApi.Tests.CustomerController;

public class GetAllCustomersTests
{
    [Fact]
    public async Task Valid_ReturnsOk()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"), "bob@bobertson.co.uk", "Bob", "Bobertson");
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/AnotherExistingUser"), "bobetta@bobson.co.uk", "Bobetta", "Bobson");
        
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");
        
        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customers = await response.Content.ReadFromJsonAsync<List<CustomerResponse>>();

        customers.Should().NotBeNull();
        customers.Should().HaveCount(2);

        var bob = customers!.FirstOrDefault(c => c.CustomerUri == "/customers/ExistingUser");
        bob.Should().NotBeNull();
        bob!.EmailAddress.Should().Be("bob@bobertson.co.uk");
        bob.FirstName.Should().Be("Bob");
        bob.LastName.Should().Be("Bobertson");
        bob.CustomerUri.Should().Be("/customers/ExistingUser");
        bob.CreatedOn.Should().BeWithin(TimeSpan.FromSeconds(1)).Before(DateTimeOffset.UtcNow);
        
        var bobetta = customers!.FirstOrDefault(c => c.CustomerUri == "/customers/AnotherExistingUser");
        bobetta.Should().NotBeNull();
        bobetta!.EmailAddress.Should().Be("bobetta@bobson.co.uk");
        bobetta.FirstName.Should().Be("Bobetta");
        bobetta.LastName.Should().Be("Bobson");
        bobetta.CustomerUri.Should().Be("/customers/AnotherExistingUser");
        bobetta.CreatedOn.Should().BeWithin(TimeSpan.FromSeconds(1)).Before(DateTimeOffset.UtcNow);
    }
    
    [Fact]
    public async Task Valid_NoCustomers_ReturnsOk()
    {
        using var customerApi = new TestCustomerApi();

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");
        
        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customers = await response.Content.ReadFromJsonAsync<List<CustomerResponse>>();

        customers.Should().NotBeNull();
        customers.Should().HaveCount(0);
    }
    
    [Fact]
    public async Task Unauthorized()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"));

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Forbidden()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"));
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.InvalidRole");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}