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

    public async Task TheCustomerShouldMatch(CustomerUri customerUri, Expression<Func<CustomerReadModel, bool>> matchFunc)
    {
        await customerStore.ThenTheCustomerShouldMatch(customerUri, matchFunc);
    }

    public async Task TheMovieShouldMatch(MovieUri movieUri, Expression<Func<Movie, bool>> matchFunc)
    {
        await customerStore.ThenTheMovieShouldMatch(movieUri, matchFunc);
    }

    public async Task TheMovieSnapshotShouldMatch(MovieUri movieUri, Expression<Func<MovieReadModel, bool>> matchFunc)
    {
        await movieStore.ThenTheMovieSnapshotShouldMatch(movieUri, matchFunc);
    }

    public async Task TheSongShouldMatch(SongUri songUri, Expression<Func<Song, bool>> matchFunc)
    {
        await customerStore.ThenTheSongShouldMatch(songUri, matchFunc);
    }

    public async Task TheSearchableInterestShouldMatch(string id, Expression<Func<SearchableInterest, bool>> matchFunc)
    {
        await searchableInterestStore.ThenTheSearchableInterestShouldMatch(id, matchFunc);
    }
}
