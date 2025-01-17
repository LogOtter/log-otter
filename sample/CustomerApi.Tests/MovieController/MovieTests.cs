using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.Movies;
using CustomerApi.Uris;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CustomerApi.Tests.MovieController;

public class MovieTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Valid_ReturnsOkWhenUsingEventStream()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        var movieUri = MovieUri.Parse("/movies/ExistingMovie");
        await customerApi.Given.AnExistingMovie(movieUri, "The Matrix");

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/movies/ExistingMovie/by-event");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var movie = await response.Content.ReadFromJsonAsync<MovieQueryResponse>();
        movie.ShouldNotBeNull();
        movie!.MovieUri.ShouldBe(movieUri);
        movie.Name.ShouldBe("The Matrix");
    }

    [Fact]
    public async Task Valid_ReturnsNotFoundWhenUsingSnapshotThatIsNotEnabled()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        var movieUri = MovieUri.Parse("/movies/ExistingMovie");
        await customerApi.Given.AnExistingMovie(movieUri, "The Matrix");

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/movies/ExistingMovie/by-snapshot");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Valid_ReturnsOkWhenUsingHybridAndSnapshotIsNotEnabled()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        var movieUri = MovieUri.Parse("/movies/ExistingMovie");
        await customerApi.Given.AnExistingMovie(movieUri, "The Matrix");

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/movies/ExistingMovie/by-hybrid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var movie = await response.Content.ReadFromJsonAsync<MovieQueryResponse>();
        movie.ShouldNotBeNull();
        movie!.MovieUri.ShouldBe(movieUri);
        movie.Name.ShouldBe("The Matrix");
    }

    [Fact]
    public async Task Valid_SavesSnapshotWhenUsingHybridRepoToApply()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        var client = customerApi.CreateClient();

        var response = await client.PostAsJsonAsync("/movies/create-hybrid", new { name = "Hot Fuzz" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        var movieUri = MovieUri.Parse(response.Headers.Location!.ToString());
        await customerApi.Then.TheMovieSnapshotShouldMatch(movieUri, snapshot => snapshot.Name.ShouldBe("Hot Fuzz"));
        var movie = await response.Content.ReadFromJsonAsync<MovieQueryResponse>();
        movie.ShouldNotBeNull();
        movie!.MovieUri.ShouldBe(movieUri);
        movie.Name.ShouldBe("Hot Fuzz");
    }

    [Fact]
    public async Task Valid_ReturnsOkWhenUsingHybridToQueryLatestChanges()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        var movieUri = MovieUri.Parse("/movies/ExistingMovie");
        await customerApi.Given.AnExistingMovieWithAProjectedSnapshot(movieUri, "The Matrix");
        await customerApi.Given.AnExistingMovieNameIsChangedButNotProjected(movieUri, "The Matrix Reloaded");

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/movies/ExistingMovie/by-hybrid-query");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var movie = await response.Content.ReadFromJsonAsync<MovieQueryResponse>();
        movie.ShouldNotBeNull();
        movie!.MovieUri.ShouldBe(movieUri);
        movie.Name.ShouldBe("The Matrix Reloaded");
    }

    [Fact]
    public async Task Valid_ReturnsStreamChanges()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);
        var movieUri = MovieUri.Parse("/movies/ExistingMovie");
        await customerApi.Given.AnExistingMovie(movieUri, "The Matrix");
        await customerApi.Given.AnExistingMovieNameIsChangedButNotProjected(movieUri, "The Matrix Reloaded");

        var client = customerApi.CreateClient();

        var response = await client.GetAsync("/movies/ExistingMovie/history");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var movieHistory = await response.Content.ReadFromJsonAsync<MovieHistoryResponse>();
        movieHistory.ShouldNotBeNull();
        movieHistory.ChangeDescriptions.Count.ShouldBe(2);
        movieHistory.ChangeDescriptions.ShouldContain("Movie The Matrix added");
        movieHistory.ChangeDescriptions.ShouldContain("Movie /movies/ExistingMovie name changed to The Matrix Reloaded");
    }
}
