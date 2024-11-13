using System.Linq.Expressions;
using System.Net;
using Bogus;
using CustomerApi.Events.Customers;
using CustomerApi.NonEventSourcedData.CustomerInterests;
using CustomerApi.Services;
using CustomerApi.Uris;
using FluentAssertions;
using LogOtter.CosmosDb;
using LogOtter.CosmosDb.ContainerMock;
using LogOtter.CosmosDb.EventStore;
using Microsoft.Azure.Cosmos;

// ReSharper disable UnusedMethodReturnValue.Local

namespace CustomerApi.Tests;

public class CustomerStore(
    EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository,
    EmailAddressReservationService emailAddressReservationService,
    SnapshotRepository<CustomerEvent, CustomerReadModel> customerSnapshotRepository,
    CosmosContainer<Movie> movieContainer,
    CosmosContainer<Song> songContainer,
    CosmosContainer<CustomerEvent> customerEventContainer
)
{
    private readonly Container _movieContainer = movieContainer.Container;
    private readonly Container _songContainer = songContainer.Container;

    public async Task<CustomerReadModel> GivenAnExistingCustomer(
        CustomerUri customerUri,
        Discretionary<string> emailAddress,
        Discretionary<string> firstName,
        Discretionary<string> lastName
    )
    {
        var customerReadModel = await customerEventRepository.Get(customerUri.Uri);
        if (customerReadModel != null)
        {
            return customerReadModel;
        }

        var fakePerson = new Person("en_GB");

        var customerCreated = new CustomerCreated(
            customerUri,
            emailAddress.GetValueOrDefault(fakePerson.Email),
            firstName.GetValueOrDefault(fakePerson.FirstName),
            lastName.GetValueOrDefault(fakePerson.LastName)
        );

        await emailAddressReservationService.ReserveEmailAddress(customerCreated.EmailAddress);
        return await customerEventRepository.ApplyEvents(customerUri.Uri, 0, customerCreated);
    }

    public async Task<CustomerReadModel> AnExistingCustomerEmailWasUpdated(CustomerUri customerUri, string emailAddress)
    {
        var customerReadModel = await customerEventRepository.Get(customerUri.Uri);
        var emailUpdated = new CustomerEmailAddressChanged(customerUri, customerReadModel!.EmailAddress, emailAddress);
        return await customerEventRepository.ApplyEvents(customerUri.Uri, customerReadModel.Revision, emailUpdated);
    }

    public async Task GivenTheCustomerIsDeleted(CustomerUri customerUri)
    {
        var customerDeleted = new CustomerDeleted(customerUri);
        await ApplyEvents(customerDeleted);
    }

    public async Task ThenTheCustomerShouldBeDeleted(CustomerUri customerUri)
    {
        var customerReadModel = await customerSnapshotRepository.GetSnapshot(customerUri.Uri, CustomerReadModel.StaticPartitionKey);

        customerReadModel.Should().BeNull("The customer should not exist in the read store");

        var customerQueryReadModel = (
            await customerSnapshotRepository.QuerySnapshots(CustomerReadModel.StaticPartitionKey).ToListAsync()
        ).SingleOrDefault(c => c.CustomerUri == customerUri);

        customerQueryReadModel.Should().BeNull("The customer should not exist in the query read store");

        var customerWriteModel = await customerEventRepository.Get(customerUri.Uri);
        customerWriteModel.Should().BeNull("The customer should not exist in the write store");
    }

    private async Task<CustomerReadModel> ApplyEvents(params CustomerEvent[] events)
    {
        var customerUri = events.First().CustomerUri;
        var customer = await customerEventRepository.Get(customerUri.Uri);
        var revision = customer?.Revision ?? 0;

        return await customerEventRepository.ApplyEvents(customerUri.Uri, revision, events);
    }

    public async Task ThenTheCustomerShouldMatch(CustomerUri customerUri, Expression<Func<CustomerReadModel, bool>> matchFunc)
    {
        var customerWrite = await customerEventRepository.Get(customerUri.Uri);
        customerWrite.Should().NotBeNull();
        customerWrite.Should().Match(matchFunc);

        var customerRead = await customerSnapshotRepository.GetSnapshot(customerUri.Uri, CustomerReadModel.StaticPartitionKey);
        customerRead.Should().NotBeNull();
        customerRead.Should().Match(matchFunc);

        var customerQueryRead = (await customerSnapshotRepository.QuerySnapshots(CustomerReadModel.StaticPartitionKey).ToListAsync()).SingleOrDefault(
            c => c.CustomerUri == customerUri
        );
        customerQueryRead.Should().NotBeNull();
        customerQueryRead.Should().Match(matchFunc);
    }

    public async Task ThenTheMovieShouldMatch(MovieUri movieUri, Expression<Func<Movie, bool>> matchFunc)
    {
        var movie = await _movieContainer.ReadItemAsync<Movie>(movieUri.MovieId, new PartitionKey(Movie.StaticPartition));
        movie.Resource.Should().NotBeNull();
        movie.Resource.Should().Match(matchFunc);
    }

    public async Task ThenTheSongShouldMatch(SongUri songUri, Expression<Func<Song, bool>> matchFunc)
    {
        var song = await _songContainer.ReadItemAsync<Song>(songUri.SongId, new PartitionKey(Song.StaticPartition));
        song.Resource.Should().NotBeNull();
        song.Resource.Should().Match(matchFunc);
    }

    public void GivenCreatingACustomerWillConflict()
    {
        var containerMock = customerEventContainer.Container as ContainerMock;
        containerMock!.QueueExceptionToBeThrown(
            new CosmosException("Conflict, oh no!", HttpStatusCode.Conflict, 0, string.Empty, 0),
            i => i.MethodName == "CreateItemStreamAsync"
        );
    }
}
