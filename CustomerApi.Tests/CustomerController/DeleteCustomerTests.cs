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
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/CustomerId"));
        var client = customerApi.CreateClient();

        var response = await client.DeleteAsync("/customers/CustomerId");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
    
    [Fact]
    public async Task ExistingDeleted_ReturnsNoContent()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/CustomerId"));
        await customerApi.Given.TheCustomerIsDeleted(CustomerUri.Parse("/customers/CustomerId"));
        var client = customerApi.CreateClient();

        var response = await client.DeleteAsync("/customers/CustomerId");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}