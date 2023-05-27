using CustomerApi.Uris;

namespace CustomerApi.NonEventSourcedData.CustomerInterests;

public record Movie(MovieUri MovieUri, string Name, int RuntimeMinutes) : CustomerInterest(MovieUri.MovieId, MovieUri.Uri, Name, StaticPartition)
{
    public const string StaticPartition = "/movies";
}
