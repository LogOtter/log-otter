using CustomerApi.Events.Movies;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Movies;

[ApiController]
[Route("movies")]
public class MovieController : ControllerBase
{
    private readonly EventRepository<MovieEvent, MovieReadModel> _movieEventRepository;
    private readonly SnapshotRepository<MovieEvent, MovieReadModel> _movieSnapshotRepository;
    private readonly HybridRepository<MovieEvent, MovieReadModel> _movieHybridRepository;

    public MovieController(
        EventRepository<MovieEvent, MovieReadModel> movieEventRepository,
        SnapshotRepository<MovieEvent, MovieReadModel> movieSnapshotRepository,
        HybridRepository<MovieEvent, MovieReadModel> movieHybridRepository
    )
    {
        _movieEventRepository = movieEventRepository;
        _movieSnapshotRepository = movieSnapshotRepository;
        _movieHybridRepository = movieHybridRepository;
    }

    [HttpGet("{movieId}/by-event")]
    public async Task<IActionResult> GetByEvent(string movieId, CancellationToken cancellationToken)
    {
        var movieUri = new MovieUri(movieId);
        var movie = await _movieEventRepository.Get(movieUri.Uri, cancellationToken: cancellationToken);

        if (movie != null)
        {
            return Ok(movie);
        }

        return NotFound();
    }

    [HttpGet("{movieId}/history")]
    public async Task<IActionResult> GetMovieHistory(string movieId, CancellationToken cancellationToken)
    {
        var movieUri = new MovieUri(movieId);
        var movieEvents = await _movieEventRepository.GetEventStream(movieUri.Uri, cancellationToken: cancellationToken);

        var history = movieEvents.Select(e => e.GetDescription() ?? "").ToList();

        return Ok(new MovieHistoryResponse(history));
    }

    [HttpGet("{movieId}/by-snapshot")]
    public async Task<IActionResult> GetBySnapshot(string movieId, CancellationToken cancellationToken)
    {
        var movieUri = new MovieUri(movieId);
        var movie = await _movieSnapshotRepository.GetSnapshot(movieUri.Uri, MovieReadModel.StaticPartitionKey, cancellationToken: cancellationToken);

        if (movie != null)
        {
            return Ok(movie);
        }

        return NotFound();
    }

    [HttpGet("{movieId}/by-hybrid")]
    public async Task<IActionResult> GetByHybrid(string movieId, CancellationToken cancellationToken)
    {
        var movieUri = new MovieUri(movieId);
        var movie = await _movieHybridRepository.GetSnapshotWithCatchupExpensivelyAsync(
            movieUri.Uri,
            MovieReadModel.StaticPartitionKey,
            cancellationToken: cancellationToken
        );

        if (movie != null)
        {
            return Ok(movie);
        }

        return NotFound();
    }

    [HttpGet("{movieId}/by-hybrid-query")]
    public async Task<IActionResult> GetByHybridQuery(string movieId, CancellationToken cancellationToken)
    {
        var movieUri = new MovieUri(movieId);

        var movies = await _movieHybridRepository
            .QuerySnapshotsWithCatchupExpensivelyAsync(
                MovieReadModel.StaticPartitionKey,
                q => q.Where(m => m.MovieUri.Uri == movieUri.Uri),
                cancellationToken: cancellationToken
            )
            .ToListAsync();

        var movie = movies.FirstOrDefault();
        if (movie != null)
        {
            return Ok(new MovieQueryResponse(movie.MovieUri, movie.Name));
        }

        return NotFound();
    }

    [HttpPost("create-hybrid")]
    public async Task<IActionResult> CreateUsingHybrid(CreateMovieRequest request, CancellationToken cancellationToken)
    {
        var movieUri = MovieUri.Generate();

        var movieCreated = new MovieAdded(movieUri, request.Name);
        var movie = await _movieHybridRepository.ApplyEventsAndUpdateSnapshotImmediately(movieUri.Uri, 0, cancellationToken, movieCreated);

        return Created(movieUri.Uri, movie);
    }
}
