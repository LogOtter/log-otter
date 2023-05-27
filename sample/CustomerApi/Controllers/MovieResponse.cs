using System.Diagnostics.CodeAnalysis;
using CustomerApi.Services.CustomerInterests;

namespace CustomerApi.Controllers;

public record MovieResponse
{
    public required string MovieUri { get; init; }
    public required string Name { get; init; }
    public required int RuntimeMinutes { get; init; }

    [SetsRequiredMembers]
    public MovieResponse(Movie movie)
    {
        MovieUri = movie.MovieUri.Uri;
        Name = movie.Name;
        RuntimeMinutes = movie.RuntimeMinutes;
    }
}
