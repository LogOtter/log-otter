using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Customers.Create;
using FluentAssertions;
using Xunit;

namespace CustomerApi.Tests.CustomerController;

public class CreateCustomerTests
{
    [Fact]
    public async Task CreateCompany_ReturnsOk()
    {
        using var customerApi = new TestCustomerApi();
        var client = customerApi.CreateClient();
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}