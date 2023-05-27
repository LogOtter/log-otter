using CustomerApi.Uris;

namespace CustomerApi.Services.CustomerInterests;

public record Movie(MovieUri MovieUri, string Name, int RuntimeMinutes) : CustomerInterest(MovieUri.MovieId, Name, StaticPartition)
{
    public const string StaticPartition = "/movies";
}
