using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Movies;

public class MovieNameChanged(MovieUri movieUri, string name) : MovieEvent(movieUri)
{
    public string Name { get; } = name;

    public override void Apply(MovieReadModel model, EventInfo eventInfo)
    {
        model.Name = Name;
        model.NameVersions.Add(eventInfo.EventNumber, Name);
    }

    public override string GetDescription()
    {
        return $"Movie {MovieUri} name changed to {Name}";
    }
}
