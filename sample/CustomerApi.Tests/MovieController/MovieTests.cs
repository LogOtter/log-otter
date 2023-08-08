using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Movies;
using CustomerApi.Uris;
using FluentAssertions;
using Xunit;

namespace CustomerApi.Tests.MovieController;

public class MovieTests
{
    [Fact]
    public async Task Valid_ReturnsOkWhenUsingEventStream()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingMovie(MovieUri.Parse("/movies/ExistingMovie"), "The Matrix");

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/movies/ExistingMovie/by-event");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Valid_ReturnsNotFoundWhenUsingSnapshotThatIsNotEnabled()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingMovie(MovieUri.Parse("/movies/ExistingMovie"), "The Matrix");

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/movies/ExistingMovie/by-snapshot");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Valid_ReturnsOkWhenUsingHybridAndSnapshotIsNotEnabled()
    {
        using var customerApi = new TestCustomerApi();
        await customerApi.Given.AnExistingMovie(MovieUri.Parse("/movies/ExistingMovie"), "The Matrix");

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/movies/ExistingMovie/by-event");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Valid_SavesSnapshotWhenUsingHybridRepoToApply()
    {
        using var customerApi = new TestCustomerApi();
        var client = customerApi.CreateClient();

        var response = await client.PostAsJsonAsync("/movies/create-hybrid", new { name = "Hot Fuzz" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var movieUri = MovieUri.Parse(response.Headers.Location!.ToString());
        await customerApi.Then.TheMovieSnapshotShouldMatch(movieUri, snapshot => snapshot.Name == "Hot Fuzz");
    }

    [Fact]
    public async Task Valid_ReturnsOkWhenUsingHybridToQueryLatestChanges()
    {
        using var customerApi = new TestCustomerApi();
        var movieUri = MovieUri.Parse("/movies/ExistingMovie");
        await customerApi.Given.AnExistingMovieWithAProjectedSnapshot(movieUri, "The Matrix");
        await customerApi.Given.AnExistingMovieNameIsChangedButNotProjected(movieUri, "The Matrix Reloaded");

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/movies/ExistingMovie/by-hybrid-query");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movie = await response.Content.ReadFromJsonAsync<MovieQueryResponse>();
        movie.Should().NotBeNull();
        movie!.Name.Should().Be("The Matrix Reloaded");
    }
}
