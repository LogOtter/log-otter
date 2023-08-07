using CustomerApi.Events.Movies;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;

// ReSharper disable UnusedMethodReturnValue.Local

namespace CustomerApi.Tests;

public class MovieStore
{
    private readonly EventRepository<MovieEvent, MovieReadModel> _movieEventRepository;

    public MovieStore(EventRepository<MovieEvent, MovieReadModel> movieEventRepository)
    {
        _movieEventRepository = movieEventRepository;
    }

    public async Task<MovieReadModel> GivenAnExistingMovie(MovieUri movieUri, Discretionary<string> name)
    {
        var movieReadModel = await _movieEventRepository.Get(movieUri.Uri);
        if (movieReadModel != null)
        {
            return movieReadModel;
        }

        var movieAdded = new MovieAdded(movieUri, name.GetValueOrDefault("Dwayne Dibley in the Duke of Dork"));

        return await _movieEventRepository.ApplyEvents(movieUri.Uri, 0, movieAdded);
    }
}
