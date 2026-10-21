using System.Net;
using CustomerApi.Events.Customers;
using CustomerApi.Uris;
using Shouldly;
using Xunit;

namespace CustomerApi.Tests.CustomerController;

public class CompactCustomerTests
{
    [Fact]
    public async Task NoExisting_ReturnsNotFound()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Compact");
        var client = customerApi.CreateClient(authHeader);

        var response = await client.PostAsync("/customers/CustomerId/compact", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Existing_ReturnsNoContent()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Compact");
        await customerApi.Given.AnExistingCustomer(customerUri);
        var client = customerApi.CreateClient(authHeader);

        var response = await client.PostAsync("/customers/CustomerId/compact", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Existing_PiiIsScrubbedFromReadModel()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Compact");
        await customerApi.Given.AnExistingCustomer(customerUri, emailAddress: "bob@bobertson.co.uk", firstName: "Bob", lastName: "Bobertson");
        var client = customerApi.CreateClient(authHeader);

        await client.PostAsync("/customers/CustomerId/compact", content: null, TestContext.Current.CancellationToken);

        await customerApi.Then.TheCustomerShouldMatch(
            customerUri,
            c => c.EmailAddress.ShouldBe(CustomerStreamCompactor.RedactedValue),
            c => c.FirstName.ShouldBe(CustomerStreamCompactor.RedactedValue),
            c => c.LastName.ShouldBe(CustomerStreamCompactor.RedactedValue)
        );
    }

    [Fact]
    public async Task Existing_OriginalEventsAreDeleted()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Compact");
        await customerApi.Given.AnExistingCustomer(customerUri, emailAddress: "bob@bobertson.co.uk", firstName: "Bob", lastName: "Bobertson");
        await customerApi.Given.TheCustomerNameIsChanged(customerUri, "Bobby", "Bobertson");
        var client = customerApi.CreateClient(authHeader);

        await client.PostAsync("/customers/CustomerId/compact", content: null, TestContext.Current.CancellationToken);

        await customerApi.Then.TheCustomerStreamShouldOnlyContainTombstone(customerUri);
    }

    [Fact]
    public async Task Existing_CreatedOnIsPreserved()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Compact");
        var original = await customerApi.Given.AnExistingCustomer(
            customerUri,
            emailAddress: "bob@bobertson.co.uk",
            firstName: "Bob",
            lastName: "Bobertson"
        );
        var client = customerApi.CreateClient(authHeader);

        await client.PostAsync("/customers/CustomerId/compact", content: null, TestContext.Current.CancellationToken);

        await customerApi.Then.TheCustomerShouldMatch(customerUri, c => c.CreatedOn.ShouldBe(original.CreatedOn));
    }

    [Fact]
    public async Task ExistingDeleted_ReturnsNoContent()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Compact");
        await customerApi.Given.AnExistingCustomer(customerUri);
        await customerApi.Given.TheCustomerIsDeleted(customerUri);
        var client = customerApi.CreateClient(authHeader);

        var response = await client.PostAsync("/customers/CustomerId/compact", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Idempotent_RecompactingIsSafe()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Compact");
        await customerApi.Given.AnExistingCustomer(customerUri, emailAddress: "bob@bobertson.co.uk", firstName: "Bob", lastName: "Bobertson");
        var client = customerApi.CreateClient(authHeader);

        await client.PostAsync("/customers/CustomerId/compact", content: null, TestContext.Current.CancellationToken);
        var response = await client.PostAsync("/customers/CustomerId/compact", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await customerApi.Then.TheCustomerStreamShouldOnlyContainTombstone(customerUri);
    }

    [Fact]
    public async Task Unauthorized()
    {
        using var customerApi = new TestCustomerApi();
        var client = customerApi.CreateClient();

        var response = await client.PostAsync("/customers/CustomerId/compact", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forbidden()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.InvalidRole");
        var client = customerApi.CreateClient(authHeader);

        var response = await client.PostAsync("/customers/CustomerId/compact", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
