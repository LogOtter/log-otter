using System.Linq.Expressions;
using CustomerApi.Events.Customers;
using CustomerApi.Events.Movies;
using CustomerApi.NonEventSourcedData.CustomerInterests;
using CustomerApi.Uris;

namespace CustomerApi.Tests;

public class ThenSteps(CustomerStore customerStore, SearchableInterestStore searchableInterestStore, MovieStore movieStore)
{
    public async Task TheCustomerShouldBeDeleted(CustomerUri customerUri)
    {
        await customerStore.ThenTheCustomerShouldBeDeleted(customerUri);
    }

    public async Task TheCustomerShouldMatch(CustomerUri customerUri, params Action<CustomerReadModel>[] conditions)
    {
        await customerStore.ThenTheCustomerShouldMatch(customerUri, conditions);
    }

    public async Task TheMovieShouldMatch(MovieUri movieUri, params Action<Movie>[] conditions)
    {
        await customerStore.ThenTheMovieShouldMatch(movieUri, conditions);
    }

    public async Task TheMovieSnapshotShouldMatch(MovieUri movieUri, params Action<MovieReadModel>[] conditions)
    {
        await movieStore.ThenTheMovieSnapshotShouldMatch(movieUri, conditions);
    }

    public async Task TheSongShouldMatch(SongUri songUri, params Action<Song>[] conditions)
    {
        await customerStore.ThenTheSongShouldMatch(songUri, conditions);
    }

    public async Task TheSearchableInterestShouldMatch(string id, params Action<SearchableInterest>[] conditions)
    {
        await searchableInterestStore.ThenTheSearchableInterestShouldMatch(id, conditions);
    }
}
