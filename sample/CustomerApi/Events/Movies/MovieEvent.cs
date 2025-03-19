using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Movies;

public abstract class MovieEvent(MovieUri movieUri) : IEvent<MovieReadModel>
{
    public MovieUri MovieUri { get; } = movieUri;

    public string EventStreamId => MovieUri.Uri;

    public abstract void Apply(MovieReadModel model, EventInfo eventInfo);

    [EventDescription]
    public abstract string? GetDescription();
}
