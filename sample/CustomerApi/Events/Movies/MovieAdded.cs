using CustomerApi.Uris;

namespace CustomerApi.Events.Movies;

public class MovieAdded : MovieEvent
{
    public string Name { get; }

    public MovieAdded(MovieUri movieUri, string name, DateTimeOffset? timestamp = null)
        : base(movieUri, timestamp)
    {
        Name = name;
    }

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
