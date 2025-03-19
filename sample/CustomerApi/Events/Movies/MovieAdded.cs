using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Movies;

public class MovieAdded(MovieUri movieUri, string name) : MovieEvent(movieUri)
{
    public string Name { get; } = name;

    public override void Apply(MovieReadModel model, EventInfo eventInfo)
    {
        model.MovieUri = MovieUri;
        model.Name = Name;
        model.CreatedOn = eventInfo.CreatedOn;
        model.NameVersions.Add(eventInfo.EventNumber, Name);
    }

    public override string GetDescription()
    {
        return $"Movie {Name} added";
    }
}
