using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Customers;
using CustomerApi.Uris;
using LogOtter.JsonHal;
using Shouldly;
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
        var createdTimestamp = DateTime.UtcNow;

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var customersResponse = await response.Content.ReadFromJsonAsync<CustomersResponse>();

        customersResponse.ShouldNotBeNull();
        customersResponse!.Customers.ShouldNotBeNull();
        customersResponse.Customers.Count.ShouldBe(2);

        customersResponse.Links.ShouldNotBeNull();
        customersResponse.Links.Count.ShouldBe(3);
        customersResponse.Links.GetFirstHref().ShouldBe(new Uri(customerApi.BaseAddress, "/customers?page=1").ToString());
        customersResponse.Links.GetSelfHref().ShouldBeEquivalentTo(new Uri(customerApi.BaseAddress, "/customers?page=1").ToString());
        customersResponse.Links.GetLastHref().ShouldBeEquivalentTo(new Uri(customerApi.BaseAddress, "/customers?page=1").ToString());

        var bob = customersResponse.Customers.FirstOrDefault(c => c.CustomerUri == "/customers/ExistingUser");
        bob.ShouldNotBeNull();
        bob!.EmailAddress.ShouldBe("bob@bobertson.co.uk");
        bob.FirstName.ShouldBe("Bob");
        bob.LastName.ShouldBe("Bobertson");
        bob.CustomerUri.ShouldBe("/customers/ExistingUser");
        bob.CreatedOn.ShouldBe(createdTimestamp, TimeSpan.FromSeconds(2));

        var bobetta = customersResponse.Customers.FirstOrDefault(c => c.CustomerUri == "/customers/AnotherExistingUser");
        bobetta.ShouldNotBeNull();
        bobetta!.EmailAddress.ShouldBe("bobetta@bobson.co.uk");
        bobetta.FirstName.ShouldBe("Bobetta");
        bobetta.LastName.ShouldBe("Bobson");
        bobetta.CustomerUri.ShouldBe("/customers/AnotherExistingUser");
        bobetta.CreatedOn.ShouldBe(createdTimestamp, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Valid_MultiplePages_Page1()
    {
        using var customerApi = new TestCustomerApi();
        for (var i = 0; i < 10; i++)
        {
            await customerApi.Given.AnExistingCustomer(CustomerUri.Parse($"/customers/ExistingUser{i}"));
        }

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var customersResponse = await response.Content.ReadFromJsonAsync<CustomersResponse>();

        customersResponse.ShouldNotBeNull();
        customersResponse!.Customers.ShouldNotBeNull();

        // TestCustomerApi is configured to have a page size of 5
        customersResponse.Customers.Count.ShouldBe(5);

        customersResponse.Links.ShouldNotBeNull();
        customersResponse.Links.Count.ShouldBe(4);
        customersResponse.Links.GetFirstHref().ShouldBe(new Uri(customerApi.BaseAddress, "/customers?page=1").ToString());
        customersResponse.Links.GetSelfHref().ShouldBe(new Uri(customerApi.BaseAddress, "/customers?page=1").ToString());
        customersResponse.Links.GetNextHref().ShouldBe(new Uri(customerApi.BaseAddress, "/customers?page=2").ToString());
        customersResponse.Links.GetLastHref().ShouldBeEquivalentTo(new Uri(customerApi.BaseAddress, "/customers?page=2").ToString());
    }

    [Fact]
    public async Task Valid_MultiplePages_Page2()
    {
        using var customerApi = new TestCustomerApi();
        for (var i = 0; i < 20; i++)
        {
            await customerApi.Given.AnExistingCustomer(CustomerUri.Parse($"/customers/ExistingUser{i}"));
        }

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers?page=2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var customersResponse = await response.Content.ReadFromJsonAsync<CustomersResponse>();

        customersResponse.ShouldNotBeNull();
        customersResponse!.Customers.ShouldNotBeNull();

        // TestCustomerApi is configured to have a page size of 5
        customersResponse.Customers.Count.ShouldBe(5);

        customersResponse.Links.ShouldNotBeNull();
        customersResponse.Links.Count.ShouldBe(5);
        customersResponse.Links.GetFirstHref().ShouldBe(new Uri(customerApi.BaseAddress, "/customers?page=1").ToString());
        customersResponse.Links.GetPrevHref().ShouldBe(new Uri(customerApi.BaseAddress, "/customers?page=1").ToString());
        customersResponse.Links.GetSelfHref().ShouldBe(new Uri(customerApi.BaseAddress, "/customers?page=2").ToString());
        customersResponse.Links.GetNextHref().ShouldBe(new Uri(customerApi.BaseAddress, "/customers?page=3").ToString());
        customersResponse.Links.GetLastHref().ShouldBeEquivalentTo(new Uri(customerApi.BaseAddress, "/customers?page=4").ToString());
    }

    [Fact]
    public async Task Valid_NoCustomers_ReturnsOk()
    {
        using var customerApi = new TestCustomerApi();

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var customerResponse = await response.Content.ReadFromJsonAsync<CustomersResponse>();

        customerResponse.ShouldNotBeNull();
        customerResponse!.Customers.ShouldNotBeNull();
        customerResponse.Customers.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-2147483648")]
    [InlineData("-2147483649")]
    [InlineData("2147483648")]
    [InlineData("foo")]
    public async Task Invalid_Page(string page)
    {
        using var customerApi = new TestCustomerApi();

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync($"/customers?page={page}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthorized()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"));

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/customers");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forbidden()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(CustomerUri.Parse("/customers/ExistingUser"));
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.InvalidRole");

        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
