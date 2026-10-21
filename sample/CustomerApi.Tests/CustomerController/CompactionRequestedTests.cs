using CustomerApi.Events.Customers;
using CustomerApi.Uris;
using Shouldly;
using Xunit;

namespace CustomerApi.Tests.CustomerController;

public class CompactionRequestedTests
{
    [Fact]
    public async Task AppendingCompactionRequestedEvent_TriggersCompactionViaChangeFeed()
    {
        var customerUri = CustomerUri.Parse("/customers/CustomerId");

        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingCustomer(customerUri, emailAddress: "bob@bobertson.co.uk", firstName: "Bob", lastName: "Bobertson");
        await customerApi.Given.TheCustomerNameIsChanged(customerUri, "Bobby", "Bobertson");

        await customerApi.Given.CompactionIsRequested(customerUri);

        await customerApi.Then.TheCustomerStreamShouldOnlyContainTombstone(customerUri);
        await customerApi.Then.TheCustomerShouldMatch(
            customerUri,
            c => c.EmailAddress.ShouldBe(CustomerStreamCompactor.RedactedValue),
            c => c.FirstName.ShouldBe(CustomerStreamCompactor.RedactedValue),
            c => c.LastName.ShouldBe(CustomerStreamCompactor.RedactedValue)
        );
    }
}
