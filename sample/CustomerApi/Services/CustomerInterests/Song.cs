using CustomerApi.Uris;

namespace CustomerApi.Services.CustomerInterests;

public record Song(SongUri SongUri, string Name, string Genre) : CustomerInterest(SongUri.SongId, Name, StaticPartition)
{
    public const string StaticPartition = "/songs";
}
