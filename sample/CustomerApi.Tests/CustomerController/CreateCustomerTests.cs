using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Customers.Create;
using CustomerApi.Uris;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CustomerApi.Tests.CustomerController;

public class CreateCustomerTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Valid_ReturnsOk(bool disableAutoProvisioning)
    {
        using var customerApi = new TestCustomerApi(testOutputHelper, disableAutoProvisioning);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Valid_StoredCorrectly(bool disableAutoProvisioning)
    {
        using var customerApi = new TestCustomerApi(testOutputHelper, disableAutoProvisioning);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);
        var location = response.Headers.Location?.ToString();
        location.ShouldNotBeNull("Location should be returned");

        var customerUri = CustomerUri.Parse(location!);

        await customerApi.Then.TheCustomerShouldMatch(
            customerUri,
            c => c.EmailAddress.ShouldBe("bob@bobertson.co.uk"),
            c => c.FirstName.ShouldBe("Bob"),
            c => c.LastName.ShouldBe("Bobertson")
        );
    }

    [Fact]
    public async Task Invalid_DuplicateEmailAddress()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);

        var customerUri = CustomerUri.Parse("/customers/CustomerId");
        await customerApi.Given.AnExistingCustomer(customerUri, emailAddress: "bob@bobertson.co.uk");

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Valid_DeleteCustomer_DuplicateEmailAddress()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);

        var customerUri = CustomerUri.Parse("/customers/CustomerId");
        await customerApi.Given.AnExistingCustomer(customerUri, emailAddress: "bob@bobertson.co.uk");

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Create", "Customers.Delete");

        var client = customerApi.CreateClient(authHeader);

        await client.DeleteAsync(customerUri.Uri);

        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");
        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData(null, "Bob", "Bobertson")]
    [InlineData("", "Bob", "Bobertson")]
    [InlineData("bob@bobertson.co.uk", null, "Bobertson")]
    [InlineData("bob@bobertson.co.uk", "", "Bobertson")]
    [InlineData("bob@bobertson.co.uk", "Bob", null)]
    [InlineData("bob@bobertson.co.uk", "Bob", "")]
    [InlineData("not-an-email-address", "Bob", "Bobertson")]
    public async Task Invalid_ReturnsBadRequest(string? email, string? firstName, string? lastName)
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateCustomerRequest(email!, firstName!, lastName!);

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthorized()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);

        var client = customerApi.CreateClient();
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forbidden()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.InvalidRole");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Conflict()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");
        customerApi.Given.CreatingACustomerWillConflict();

        var response = await client.PostAsJsonAsync("/customers", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
