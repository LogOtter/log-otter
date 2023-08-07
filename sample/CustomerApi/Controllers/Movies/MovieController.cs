using CustomerApi.Events.Movies;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Movies;

[ApiController]
[Route("movies/{movieId}")]
public class MovieController : ControllerBase
{
    private readonly EventRepository<MovieEvent, MovieReadModel> _movieEventRepository;
    private readonly SnapshotRepository<MovieEvent, MovieReadModel> _movieSnapshotRepository;

    public MovieController(
        EventRepository<MovieEvent, MovieReadModel> movieEventRepository,
        SnapshotRepository<MovieEvent, MovieReadModel> movieSnapshotRepository
    )
    {
        _movieEventRepository = movieEventRepository;
        _movieSnapshotRepository = movieSnapshotRepository;
    }

    [HttpGet("by-event")]
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

    [HttpGet("by-snapshot")]
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
}
