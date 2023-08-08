using CustomerApi.Uris;

namespace CustomerApi.Events.Movies;

public class MovieNameChanged : MovieEvent
{
    public string Name { get; }

    public MovieNameChanged(MovieUri movieUri, string name, DateTimeOffset? timestamp = null)
        : base(movieUri, timestamp)
    {
        Name = name;
    }

    public override void Apply(MovieReadModel model)
    {
        model.Name = Name;
    }

    public override string GetDescription()
    {
        return $"Movie {MovieUri} name changed to {Name}";
    }
}
