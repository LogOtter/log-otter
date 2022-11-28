using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Customers;
using CustomerApi.Uris;
using FluentAssertions;
using LogOtter.JsonHal;
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
        
        var customersResponse = await response.Content.ReadFromJsonAsync<CustomersResponse>();

        customersResponse.Should().NotBeNull();
        customersResponse!.Customers.Should().NotBeNull();
        customersResponse.Customers.Should().HaveCount(2);

        customersResponse.Links.Should().NotBeNull();
        customersResponse.Links.Should().HaveCount(1);
        customersResponse.Links.First().Should().BeEquivalentTo(new JsonHalLink("self", new Uri(customerApi.BaseAddress, "/customers?page=1").ToString()));

        var bob = customersResponse.Customers.FirstOrDefault(c => c.CustomerUri == "/customers/ExistingUser");
        bob.Should().NotBeNull();
        bob!.EmailAddress.Should().Be("bob@bobertson.co.uk");
        bob.FirstName.Should().Be("Bob");
        bob.LastName.Should().Be("Bobertson");
        bob.CustomerUri.Should().Be("/customers/ExistingUser");
        bob.CreatedOn.Should().BeWithin(TimeSpan.FromSeconds(1)).Before(DateTimeOffset.UtcNow);
        
        var bobetta = customersResponse.Customers.FirstOrDefault(c => c.CustomerUri == "/customers/AnotherExistingUser");
        bobetta.Should().NotBeNull();
        bobetta!.EmailAddress.Should().Be("bobetta@bobson.co.uk");
        bobetta.FirstName.Should().Be("Bobetta");
        bobetta.LastName.Should().Be("Bobson");
        bobetta.CustomerUri.Should().Be("/customers/AnotherExistingUser");
        bobetta.CreatedOn.Should().BeWithin(TimeSpan.FromSeconds(1)).Before(DateTimeOffset.UtcNow);
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

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customersResponse = await response.Content.ReadFromJsonAsync<CustomersResponse>();

        customersResponse.Should().NotBeNull();
        customersResponse!.Customers.Should().NotBeNull();
        
        // TestCustomerApi is configured to have a page size of 5
        customersResponse.Customers.Should().HaveCount(5);

        customersResponse.Links.Should().NotBeNull();
        customersResponse.Links.Should().HaveCount(2);
        customersResponse.Links.GetSelfHref().Should().Be(new Uri(customerApi.BaseAddress, "/customers?page=1").ToString());
        customersResponse.Links.GetNextHref().Should().Be(new Uri(customerApi.BaseAddress, "/customers?page=2").ToString());
    }
    
    [Fact]
    public async Task Valid_MultiplePages_Page2()
    {
        using var customerApi = new TestCustomerApi();
        for (var i = 0; i < 15; i++)
        {
            await customerApi.Given.AnExistingCustomer(CustomerUri.Parse($"/customers/ExistingUser{i}"));    
        }
        
        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");
        
        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers?page=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customersResponse = await response.Content.ReadFromJsonAsync<CustomersResponse>();

        customersResponse.Should().NotBeNull();
        customersResponse!.Customers.Should().NotBeNull();
        
        // TestCustomerApi is configured to have a page size of 5
        customersResponse.Customers.Should().HaveCount(5);

        customersResponse.Links.Should().NotBeNull();
        customersResponse.Links.Should().HaveCount(3);
        customersResponse.Links.GetPrevHref().Should().Be(new Uri(customerApi.BaseAddress, "/customers?page=1").ToString());
        customersResponse.Links.GetSelfHref().Should().Be(new Uri(customerApi.BaseAddress, "/customers?page=2").ToString());
        customersResponse.Links.GetNextHref().Should().Be(new Uri(customerApi.BaseAddress, "/customers?page=3").ToString());
    }
    
    [Fact]
    public async Task Valid_NoCustomers_ReturnsOk()
    {
        using var customerApi = new TestCustomerApi();

        var authHeader = await customerApi.Given.AnExistingConsumer("Customers.Read");
        
        var client = customerApi.CreateClient(authHeader);

        var response = await client.GetAsync("/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customerResponse = await response.Content.ReadFromJsonAsync<CustomersResponse>();

        customerResponse.Should().NotBeNull();
        customerResponse!.Customers.Should().NotBeNull();
        customerResponse.Customers.Should().HaveCount(0);
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

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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