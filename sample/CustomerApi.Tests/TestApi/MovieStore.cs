using System.Linq.Expressions;
using CustomerApi.Events.Movies;
using CustomerApi.Uris;
using FluentAssertions;
using LogOtter.CosmosDb.EventStore;

// ReSharper disable UnusedMethodReturnValue.Local

namespace CustomerApi.Tests;

public class MovieStore(
    EventRepository<MovieEvent, MovieReadModel> movieEventRepository,
    SnapshotRepository<MovieEvent, MovieReadModel> movieSnapshotRepository,
    HybridRepository<MovieEvent, MovieReadModel> movieHybridRepository
)
{
    public async Task<MovieReadModel> GivenAnExistingMovie(MovieUri movieUri, Discretionary<string> name)
    {
        var movieReadModel = await movieEventRepository.Get(movieUri.Uri);
        if (movieReadModel != null)
        {
            return movieReadModel;
        }

        var movieAdded = new MovieAdded(movieUri, name.GetValueOrDefault("Dwayne Dibley in the Duke of Dork"));

        return await movieEventRepository.ApplyEvents(movieUri.Uri, 0, movieAdded);
    }

    public async Task<MovieReadModel> GivenAnExistingMovieSnapshot(MovieUri movieUri, Discretionary<string> name)
    {
        var movieReadModel = await movieEventRepository.Get(movieUri.Uri);
        if (movieReadModel != null)
        {
            return movieReadModel;
        }

        var movieAdded = new MovieAdded(movieUri, name.GetValueOrDefault("Dwayne Dibley in the Duke of Dork"));

        return await movieHybridRepository.ApplyEventsAndUpdateSnapshotImmediately(movieUri.Uri, 0, CancellationToken.None, movieAdded);
    }

    public async Task<MovieReadModel> GivenAnExistingMovieNameIsChanged(MovieUri movieUri, string newName)
    {
        var movieReadModel = await movieEventRepository.Get(movieUri.Uri);
        if (movieReadModel == null)
        {
            throw new Exception("Movie not found, make sure you called a setup method to create it first");
        }

        var movieNameChanged = new MovieNameChanged(movieUri, newName);
        return await movieEventRepository.ApplyEvents(movieUri.Uri, movieReadModel.Revision, movieNameChanged);
    }

    public async Task ThenTheMovieSnapshotShouldMatch(MovieUri movieUri, Expression<Func<MovieReadModel, bool>> matchFunc)
    {
        var movie = await movieSnapshotRepository.GetSnapshot(movieUri.Uri, MovieReadModel.StaticPartitionKey);
        movie.Should().NotBeNull();
        movie.Should().Match(matchFunc);
    }
}
