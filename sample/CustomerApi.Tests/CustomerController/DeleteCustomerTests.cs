using System.Net;
using CustomerApi.Uris;
using FluentAssertions;
using Xunit;

namespace CustomerApi.Tests.CustomerController;

public class DeleteCustomerTests
{
    [Fact]
    public async Task NoExisting_ReturnsNotFound()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Delete");
        var client = customerApi.CreateClient(authHeader);

        var response = await client.DeleteAsync("/customers/CustomerId");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task Existing_ReturnsNoContent()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");
        
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Delete");
        await customerApi.Given.AnExistingCustomer(customerUri);
        var client = customerApi.CreateClient(authHeader);

        var response = await client.DeleteAsync("/customers/CustomerId");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
    
    [Fact]
    public async Task ExistingDeleted_ReturnsNoContent()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");
        
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Delete");
        await customerApi.Given.AnExistingCustomer(customerUri);
        await customerApi.Given.TheCustomerIsDeleted(customerUri);
        var client = customerApi.CreateClient(authHeader);

        var response = await client.DeleteAsync("/customers/CustomerId");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
    
    [Fact]
    public async Task Existing_HasBeenRemoved()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");
        
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Delete");
        await customerApi.Given.AnExistingCustomer(customerUri);
        var client = customerApi.CreateClient(authHeader);

        await client.DeleteAsync("/customers/CustomerId");

        await customerApi.Then.TheCustomerShouldBeDeleted(customerUri);
    }
    
    [Fact]
    public async Task ExistingDeleted_HasBeenRemoved()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");
        
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Delete");
        await customerApi.Given.AnExistingCustomer(customerUri);
        await customerApi.Given.TheCustomerIsDeleted(customerUri);
        var client = customerApi.CreateClient(authHeader);

        await client.DeleteAsync("/customers/CustomerId");

        await customerApi.Then.TheCustomerShouldBeDeleted(customerUri);
    }
    
    [Fact]
    public async Task Unauthorized()
    {
        using var customerApi = new TestCustomerApi();
        var client = customerApi.CreateClient();

        var response = await client.DeleteAsync("/customers/CustomerId");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Forbidden()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.InvalidRole");
        var client = customerApi.CreateClient(authHeader);

        var response = await client.DeleteAsync("/customers/CustomerId");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}