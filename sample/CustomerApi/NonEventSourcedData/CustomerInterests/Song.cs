using CustomerApi.Uris;

namespace CustomerApi.NonEventSourcedData.CustomerInterests;

public record Song(SongUri SongUri, string Name, string Genre) : CustomerInterest(SongUri.SongId, SongUri.Uri, Name, StaticPartition)
{
    public const string StaticPartition = "/songs";
}
