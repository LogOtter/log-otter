using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Customers.Create;
using FluentAssertions;
using Xunit;

namespace CustomerApi.Tests.CustomerController;

public class CreateCustomerTests
{
    [Fact]
    public async Task Valid_ReturnsOk()
    {
        using var customerApi = new TestCustomerApi();
        var client = customerApi.CreateClient();
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
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
        var client = customerApi.CreateClient();
        var request = new CreateCustomerRequest(email, firstName, lastName);

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}