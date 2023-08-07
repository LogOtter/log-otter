using System.Net;
using CustomerApi.Uris;
using FluentAssertions;
using Xunit;

namespace CustomerApi.Tests.MovieController;

public class GetMovieTests
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
}
