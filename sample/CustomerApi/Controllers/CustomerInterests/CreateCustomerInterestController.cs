using CustomerApi.NonEventSourcedData.CustomerInterests;
using CustomerApi.Uris;
using LogOtter.CosmosDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

namespace CustomerApi.Controllers.CustomerInterests;

[ApiController]
[Route("interests")]
[Authorize(Roles = "CustomerInterests.Create")]
public class CreateCustomerInterestController(CosmosContainer<Movie> movieContainer, CosmosContainer<Song> songContainer) : ControllerBase
{
    private readonly Container _movieContainer = movieContainer.Container;
    private readonly Container _songContainer = songContainer.Container;

    [HttpPost("movies")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMovie(CreateMovieRequest request)
    {
        var movieUri = MovieUri.Generate();

        var movie = new Movie(movieUri, request.Name, request.RuntimeMinutes);
        var createdMovie = await _movieContainer.CreateItemAsync(movie);

        return Created(movieUri.Uri, new MovieResponse(createdMovie));
    }

    [HttpPost("songs")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMovie(CreateSongRequest request)
    {
        var songUri = SongUri.Generate();

        var song = new Song(songUri, request.Name, request.Genre);
        var createdSong = await _songContainer.CreateItemAsync(song);

        return Created(songUri.Uri, new SongResponse(createdSong));
    }
}
