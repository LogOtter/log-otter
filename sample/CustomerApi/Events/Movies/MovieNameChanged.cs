using CustomerApi.Uris;

namespace CustomerApi.Events.Movies;

public class MovieNameChanged(MovieUri movieUri, string name, DateTimeOffset? timestamp = null) : MovieEvent(movieUri, timestamp)
{
    public string Name { get; } = name;

    public override void Apply(MovieReadModel model)
    {
        model.Name = Name;
    }

    public override string GetDescription()
    {
        return $"Movie {MovieUri} name changed to {Name}";
    }
}
