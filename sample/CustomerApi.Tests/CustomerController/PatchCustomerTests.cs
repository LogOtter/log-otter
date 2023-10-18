using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using CustomerApi.Controllers.Customers.Create;
using CustomerApi.Uris;
using FluentAssertions;
using LogOtter.HttpPatch;
using Xunit;
using Xunit.Abstractions;

namespace CustomerApi.Tests.CustomerController;

public class PatchCustomerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public PatchCustomerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Valid_ReturnsOk()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi(_testOutputHelper);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.ReadWrite");
        await customerApi.Given.AnExistingCustomer(customerUri, "bob@bobertson.co.uk", "Bob", "Bobertson");

        var client = customerApi.CreateClient(authHeader);
        var request = new { FirstName = "Bobby" };
        var response = await client.PatchAsJsonAsync("/customers/CustomerId", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Valid_ModifiedCorrectly()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi(_testOutputHelper);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.ReadWrite");
        var existingCustomer = await customerApi.Given.AnExistingCustomer(customerUri, "bob@bobertson.co.uk", "Bob", "Bobertson");

        var client = customerApi.CreateClient(authHeader);
        var request = new { FirstName = "Bobby" };
        await client.PatchAsJsonAsync("/customers/CustomerId", request);

        await customerApi.Then.TheCustomerShouldMatch(
            customerUri,
            c =>
                c.EmailAddress == "bob@bobertson.co.uk"
                && c.FirstName == "Bobby"
                && c.LastName == "Bobertson"
                && c.Revision == existingCustomer.Revision + 1
        );
    }

    [Fact]
    public async Task Valid_ReuseEmailAddress()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi(_testOutputHelper);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.ReadWrite", "Customers.Create");
        await customerApi.Given.AnExistingCustomer(customerUri, "bob@bobertson.co.uk");

        var client = customerApi.CreateClient(authHeader);
        var request = new { EmailAddress = "bob@example.com" };
        await client.PatchAsJsonAsync("/customers/CustomerId", request);

        var createRequest = new CreateCustomerRequest("bob@bobertson.co.uk", "Bob", "Bobertson");
        var response = await client.PostAsJsonAsync("/customers", createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public static IEnumerable<object[]> InvalidData()
    {
        yield return new object[] { new OptionallyPatched<string>(true), "Bob", "Bobertson" };
        yield return new object[] { new OptionallyPatched<string>(true, ""), "Bob", "Bobertson" };
        yield return new object[] { "bob@bobertson.co.uk", new OptionallyPatched<string>(true), "Bobertson" };
        yield return new object[] { "bob@bobertson.co.uk", new OptionallyPatched<string>(true, ""), "Bobertson" };
        yield return new object[] { "bob@bobertson.co.uk", "Bob", new OptionallyPatched<string>(true) };
        yield return new object[] { "bob@bobertson.co.uk", "Bob", new OptionallyPatched<string>(true, "") };
        yield return new object[] { new OptionallyPatched<string>(true, "no-an-email-address"), "Bob", "Bobertson" };
    }

    [Theory]
    [MemberData(nameof(InvalidData))]
    public async Task Invalid_ReturnsBadRequest(
        OptionallyPatched<string> emailAddress,
        OptionallyPatched<string> firstName,
        OptionallyPatched<string> lastName
    )
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi(_testOutputHelper);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.ReadWrite");
        await customerApi.Given.AnExistingCustomer(customerUri, "bob@bobertson.co.uk", "Bob", "Bobertson");
        var client = customerApi.CreateClient(authHeader);

        var request = new JsonObject();
        if (emailAddress.IsIncludedInPatch)
        {
            request.Add("emailAddress", JsonValue.Create(emailAddress.Value));
        }

        if (firstName.IsIncludedInPatch)
        {
            request.Add("firstName", JsonValue.Create(firstName.Value));
        }

        if (lastName.IsIncludedInPatch)
        {
            request.Add("lastName", JsonValue.Create(lastName.Value));
        }

        var response = await client.PatchAsJObjectAsync("/customers/CustomerId", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SameValues_ReturnsOk()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi(_testOutputHelper);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.ReadWrite");
        var existingCustomer = await customerApi.Given.AnExistingCustomer(customerUri, "bob@bobertson.co.uk", "Bob", "Bobertson");
        var previousRevision = existingCustomer.Revision;

        var client = customerApi.CreateClient(authHeader);
        var request = new
        {
            FirstName = "Bob",
            LastName = "Bobertson",
            EmailAddress = "bob@bobertson.co.uk"
        };
        var response = await client.PatchAsJsonAsync("/customers/CustomerId", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await customerApi.Then.TheCustomerShouldMatch(
            customerUri,
            c => c.EmailAddress == "bob@bobertson.co.uk" && c.FirstName == "Bob" && c.LastName == "Bobertson" && c.Revision == previousRevision
        );
    }

    [Fact]
    public async Task NoChange_ReturnsOk()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi(_testOutputHelper);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.ReadWrite");
        await customerApi.Given.AnExistingCustomer(customerUri, "bob@bobertson.co.uk", "Bob", "Bobertson");

        var client = customerApi.CreateClient(authHeader);
        var request = new { };
        var response = await client.PatchAsJsonAsync("/customers/CustomerId", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await customerApi.Then.TheCustomerShouldMatch(
            customerUri,
            c => c.EmailAddress == "bob@bobertson.co.uk" && c.FirstName == "Bob" && c.LastName == "Bobertson"
        );
    }

    [Fact]
    public async Task Unauthorized()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi(_testOutputHelper);
        await customerApi.Given.AnExistingCustomer(customerUri, "bob@bobertson.co.uk", "Bob", "Bobertson");

        var client = customerApi.CreateClient();
        var request = new { FirstName = "Bobby" };
        var response = await client.PatchAsJsonAsync("/customers/CustomerId", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forbidden()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi(_testOutputHelper);
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.InvalidRole");
        await customerApi.Given.AnExistingCustomer(customerUri, "bob@bobertson.co.uk", "Bob", "Bobertson");

        var client = customerApi.CreateClient(authHeader);
        var request = new { FirstName = "Bobby" };
        var response = await client.PatchAsJsonAsync("/customers/CustomerId", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
