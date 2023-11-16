using CustomerApi.Uris;

namespace CustomerApi.Events.Movies;

public class MovieAdded(MovieUri movieUri, string name, DateTimeOffset? timestamp = null) : MovieEvent(movieUri, timestamp)
{
    public string Name { get; } = name;

    public override void Apply(MovieReadModel model)
    {
        model.MovieUri = MovieUri;
        model.Name = Name;
        model.CreatedOn = Timestamp;
    }

    public override string GetDescription()
    {
        return $"Movie {Name} added";
    }
}
