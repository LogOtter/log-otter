using System.Net.Mime;
using CustomerApi.Events.Movies;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Movies;

[ApiController]
[Route("movies")]
public class MovieController(
    EventRepository<MovieEvent, MovieReadModel> movieEventRepository,
    SnapshotRepository<MovieEvent, MovieReadModel> movieSnapshotRepository,
    HybridRepository<MovieEvent, MovieReadModel> movieHybridRepository
) : ControllerBase
{
    [HttpGet("{movieId}/by-event", Name = "Get a movie from the event store")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieQueryResponse>> GetByEvent(string movieId, CancellationToken cancellationToken)
    {
        var movieUri = new MovieUri(movieId);
        var movie = await movieEventRepository.Get(movieUri.Uri, cancellationToken: cancellationToken);

        if (movie != null)
        {
            return Ok(new MovieQueryResponse(movieUri, movie.Name, movie.CreatedOn, movie.Revision, movie.NameVersions));
        }

        return NotFound();
    }

    [HttpGet("{movieId}/history", Name = "Get the history of a movie from the event store")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieHistoryResponse>> GetMovieHistory(string movieId, CancellationToken cancellationToken)
    {
        var movieUri = new MovieUri(movieId);
        var movieEvents = await movieEventRepository.GetEventStream(movieUri.Uri, cancellationToken: cancellationToken);

        if (movieEvents.Count > 0)
        {
            var history = movieEvents.Select(e => e.GetDescription() ?? "").ToList();
            return Ok(new MovieHistoryResponse(history));
        }

        return NotFound();
    }

    [HttpGet("{movieId}/by-snapshot", Name = "Get a movie from the snapshot store")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieQueryResponse>> GetBySnapshot(string movieId, CancellationToken cancellationToken)
    {
        var movieUri = new MovieUri(movieId);
        var movie = await movieSnapshotRepository.GetSnapshot(movieUri.Uri, MovieReadModel.StaticPartitionKey, cancellationToken: cancellationToken);

        if (movie != null)
        {
            return Ok(new MovieQueryResponse(movieUri, movie.Name, movie.CreatedOn, movie.Revision, movie.NameVersions));
        }

        return NotFound();
    }

    [HttpGet("{movieId}/by-hybrid", Name = "Get a movie from the hybrid store")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieQueryResponse>> GetByHybrid(string movieId, CancellationToken cancellationToken)
    {
        var movieUri = new MovieUri(movieId);
        var movie = await movieHybridRepository.GetSnapshotWithCatchupExpensivelyAsync(
            movieUri.Uri,
            MovieReadModel.StaticPartitionKey,
            cancellationToken: cancellationToken
        );

        if (movie != null)
        {
            return Ok(new MovieQueryResponse(movieUri, movie.Name, movie.CreatedOn, movie.Revision, movie.NameVersions));
        }

        return NotFound();
    }

    [HttpGet("{movieId}/by-hybrid-query", Name = "Get a movie by querying the hybrid store for it")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieQueryResponse>> GetByHybridQuery(string movieId, CancellationToken cancellationToken)
    {
        var movieUri = new MovieUri(movieId);

        var movies = await movieHybridRepository
            .QuerySnapshotsWithCatchupExpensivelyAsync(
                MovieReadModel.StaticPartitionKey,
                q => q.Where(m => m.MovieUri.Uri == movieUri.Uri),
                cancellationToken: cancellationToken
            )
            .ToListAsync();

        var movie = movies.FirstOrDefault();
        if (movie != null)
        {
            return Ok(new MovieQueryResponse(movie.MovieUri, movie.Name, movie.CreatedOn, movie.Revision, movie.NameVersions));
        }

        return NotFound();
    }

    [HttpPost("create-hybrid", Name = "Create a movie using the hybrid store")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<MovieQueryResponse>> CreateUsingHybrid(CreateMovieRequest request, CancellationToken cancellationToken)
    {
        var movieUri = MovieUri.Generate();

        var movieCreated = new MovieAdded(movieUri, request.Name);
        var movie = await movieHybridRepository.ApplyEventsAndUpdateSnapshotImmediately(movieUri.Uri, 0, cancellationToken, movieCreated);

        return Created(movieUri.Uri, new MovieQueryResponse(movieUri, movie.Name, movie.CreatedOn, movie.Revision, movie.NameVersions));
    }

    [HttpPost("create", Name = "Create a movie using the event store")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<MovieQueryResponse>> Create(CreateMovieRequest request, CancellationToken cancellationToken)
    {
        var movieUri = MovieUri.Generate();

        var movieCreated = new MovieAdded(movieUri, request.Name);
        var movie = await movieEventRepository.ApplyEvents(movieUri.Uri, 0, cancellationToken, movieCreated);

        return Created(movieUri.Uri, new MovieQueryResponse(movieUri, movie.Name, movie.CreatedOn, movie.Revision, movie.NameVersions));
    }
}
