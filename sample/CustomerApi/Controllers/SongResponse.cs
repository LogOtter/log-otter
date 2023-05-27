using System.Diagnostics.CodeAnalysis;
using CustomerApi.NonEventSourcedData.CustomerInterests;

namespace CustomerApi.Controllers;

public record SongResponse
{
    public required string SongUri { get; init; }
    public required string Name { get; init; }
    public required string Genre { get; init; }

    [SetsRequiredMembers]
    public SongResponse(Song song)
    {
        SongUri = song.SongUri.Uri;
        Name = song.Name;
        Genre = song.Genre;
    }
}
