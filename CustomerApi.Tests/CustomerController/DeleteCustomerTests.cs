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
        var client = customerApi.CreateClient();

        var response = await client.DeleteAsync("/customers/CustomerId");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task Existing_ReturnsNoContent()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");
        
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(customerUri);
        var client = customerApi.CreateClient();

        var response = await client.DeleteAsync("/customers/CustomerId");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
    
    [Fact]
    public async Task ExistingDeleted_ReturnsNoContent()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");
        
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(customerUri);
        await customerApi.Given.TheCustomerIsDeleted(customerUri);
        var client = customerApi.CreateClient();

        var response = await client.DeleteAsync("/customers/CustomerId");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
    
    [Fact]
    public async Task Existing_HasBeenRemoved()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");
        
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(customerUri);
        var client = customerApi.CreateClient();

        await client.DeleteAsync("/customers/CustomerId");

        await customerApi.Then.TheCustomerShouldBeDeleted(customerUri);
    }
    
    [Fact]
    public async Task ExistingDeleted_HasBeenRemoved()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");
        
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(customerUri);
        await customerApi.Given.TheCustomerIsDeleted(customerUri);
        var client = customerApi.CreateClient();

        await client.DeleteAsync("/customers/CustomerId");

        await customerApi.Then.TheCustomerShouldBeDeleted(customerUri);
    }
}