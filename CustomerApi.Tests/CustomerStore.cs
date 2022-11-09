using Bogus;
using CustomerApi.Events.Customers;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Tests;

public class CustomerStore
{
    private static readonly Faker Faker = new("en_GB");
    
    private readonly EventRepository<CustomerEvent, CustomerReadModel> _customerEventRepository;

    public CustomerStore(EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository)
    {
        _customerEventRepository = customerEventRepository;
    }

    public async Task<CustomerReadModel> AnExistingCustomer(
        CustomerUri customerUri,
        Discretionary<string> emailAddress,
        Discretionary<string> firstName,
        Discretionary<string> lastName
    )
    {
        var customerReadModel = await _customerEventRepository.Get(customerUri.Uri);
        if (customerReadModel != null)
        {
            return customerReadModel;
        }

        var fakePerson = Faker.Person;

        var customerCreated = new CustomerCreated(
            customerUri,
            emailAddress.GetValueOrDefault(fakePerson.Email),
            firstName.GetValueOrDefault(fakePerson.FirstName),
            lastName.GetValueOrDefault(fakePerson.LastName)
        );

        return await _customerEventRepository.ApplyEvents(customerUri.Uri, 0, customerCreated);
    }

    public async Task TheCustomerIsDeleted(CustomerUri customerUri)
    {
        var customerDeleted = new CustomerDeleted(customerUri);
        await ApplyEvents(customerDeleted);
    }

    private async Task<CustomerReadModel> ApplyEvents(params CustomerEvent[] events)
    {
        var customerUri = events.First().CustomerUri;
        var customer = await _customerEventRepository.Get(customerUri.Uri);
        var revision = customer?.Revision ?? 0;

        return await _customerEventRepository.ApplyEvents(customerUri.Uri, revision, events);
    }
}