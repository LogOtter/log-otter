using System.Net;
using System.Net.Http.Json;
using CustomerApi.Controllers.CustomerInterests;
using CustomerApi.Uris;
using Shouldly;
using Xunit;

namespace CustomerApi.Tests.CustomerInterestsController;

public class CreateCustomerInterestTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidMovie_ReturnsOk(bool disableAutoProvisioning)
    {
        using var customerApi = new TestCustomerApi(disableAutoProvisioning);
        var authHeader = await customerApi.Given.AnExistingConsumer("CustomerInterests.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateMovieRequest("Alien", 116);

        var response = await client.PostAsJsonAsync("/interests/movies", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidMovie_StoredCorrectly(bool disableAutoProvisioning)
    {
        using var customerApi = new TestCustomerApi(disableAutoProvisioning);
        var authHeader = await customerApi.Given.AnExistingConsumer("CustomerInterests.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateMovieRequest("Alien", 116);

        var response = await client.PostAsJsonAsync("/interests/movies", request);
        var location = response.Headers.Location?.ToString();
        location.ShouldNotBeNull("Location should be returned");

        var movieUri = MovieUri.Parse(location!);

        await customerApi.Then.TheMovieShouldMatch(movieUri, c => c.Name.ShouldBe("Alien"), c => c.RuntimeMinutes.ShouldBe(116));
    }

    [Fact]
    public async Task ValidMovie_ProcessedToSearchableInterest()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("CustomerInterests.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateMovieRequest("Alien", 116);

        var response = await client.PostAsJsonAsync("/interests/movies", request);
        var location = response.Headers.Location?.ToString();
        location.ShouldNotBeNull("Location should be returned");

        var movieUri = MovieUri.Parse(location!);

        await customerApi.Then.TheSearchableInterestShouldMatch(movieUri.MovieId, c => c.Name.ShouldBe("Alien"), c => c.Uri.ShouldBe(movieUri.Uri));
    }

    [Fact]
    public async Task ValidSong_ReturnsOk()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("CustomerInterests.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateSongRequest("Drink", "Pirate Metal");

        var response = await client.PostAsJsonAsync("/interests/songs", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ValidSong_StoredCorrectly()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("CustomerInterests.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateSongRequest("Drink", "Pirate Metal");

        var response = await client.PostAsJsonAsync("/interests/songs", request);
        var location = response.Headers.Location?.ToString();
        location.ShouldNotBeNull("Location should be returned");

        var songUri = SongUri.Parse(location!);

        await customerApi.Then.TheSongShouldMatch(songUri, c => c.Name.ShouldBe("Drink"), c => c.Genre.ShouldBe("Pirate Metal"));
    }

    [Fact]
    public async Task ValidSong_ProcessedToSearchableInterest()
    {
        using var customerApi = new TestCustomerApi();
        var authHeader = await customerApi.Given.AnExistingConsumer("CustomerInterests.Create");

        var client = customerApi.CreateClient(authHeader);
        var request = new CreateSongRequest("Drink", "Pirate Metal");

        var response = await client.PostAsJsonAsync("/interests/songs", request);
        var location = response.Headers.Location?.ToString();
        location.ShouldNotBeNull("Location should be returned");

        var songUri = SongUri.Parse(location!);

        await customerApi.Then.TheSearchableInterestShouldMatch(songUri.SongId, c => c.Name.ShouldBe("Drink"), c => c.Uri.ShouldBe(songUri.Uri));
    }
}
