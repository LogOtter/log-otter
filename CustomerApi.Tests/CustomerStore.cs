using System.Linq.Expressions;
using Bogus;
using CustomerApi.Events.Customers;
using CustomerApi.Uris;
using FluentAssertions;
using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Tests;

public class CustomerStore
{
    private static readonly Faker Faker = new("en_GB");
    
    private readonly EventRepository<CustomerEvent, CustomerReadModel> _customerEventRepository;
    private readonly SnapshotRepository<CustomerEvent, CustomerReadModel> _customerSnapshotRepository;

    public CustomerStore(
        EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository,
        SnapshotRepository<CustomerEvent, CustomerReadModel> customerSnapshotRepository
    )
    {
        _customerEventRepository = customerEventRepository;
        _customerSnapshotRepository = customerSnapshotRepository;
    }

    public async Task<CustomerReadModel> GivenAnExistingCustomer(
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

    public async Task GivenTheCustomerIsDeleted(CustomerUri customerUri)
    {
        var customerDeleted = new CustomerDeleted(customerUri);
        await ApplyEvents(customerDeleted);
    }

    public async Task ThenTheCustomerShouldBeDeleted(CustomerUri customerUri)
    {
        var customerReadModel = await _customerSnapshotRepository
            .GetSnapshot(customerUri.Uri, CustomerReadModel.StaticPartitionKey);

        customerReadModel.Should().BeNull("The customer should not exist in the read store");
        
        var customerQueryReadModel = (await _customerSnapshotRepository
            .QuerySnapshots(CustomerReadModel.StaticPartitionKey)
            .ToListAsync())
            .SingleOrDefault(c => c.CustomerUri == customerUri);

        customerQueryReadModel.Should().BeNull("The customer should not exist in the query read store");

        var customerWriteModel = await _customerEventRepository.Get(customerUri.Uri);
        customerWriteModel.Should().BeNull("The customer should not exist in the write store");
    }

    private async Task<CustomerReadModel> ApplyEvents(params CustomerEvent[] events)
    {
        var customerUri = events.First().CustomerUri;
        var customer = await _customerEventRepository.Get(customerUri.Uri);
        var revision = customer?.Revision ?? 0;

        return await _customerEventRepository.ApplyEvents(customerUri.Uri, revision, events);
    }

    public async Task ThenTheCustomerShouldMatch(CustomerUri customerUri, Expression<Func<CustomerReadModel,bool>> matchFunc)
    {
        var customerWrite = await _customerEventRepository.Get(customerUri.Uri);
        customerWrite.Should().NotBeNull();
        customerWrite.Should().Match(matchFunc);
        
        var customerRead = await _customerSnapshotRepository.GetSnapshot(customerUri.Uri, CustomerReadModel.StaticPartitionKey);
        customerRead.Should().NotBeNull();
        customerRead.Should().Match(matchFunc);
        
        var customerQueryRead = (await _customerSnapshotRepository.QuerySnapshots(CustomerReadModel.StaticPartitionKey).ToListAsync())
            .SingleOrDefault(c => c.CustomerUri == customerUri);
        customerQueryRead.Should().NotBeNull();
        customerQueryRead.Should().Match(matchFunc);
    }
}