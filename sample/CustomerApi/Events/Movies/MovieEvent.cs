using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Movies;

public abstract class MovieEvent : IEvent<MovieReadModel>
{
    public MovieUri MovieUri { get; }

    public DateTimeOffset Timestamp { get; }

    public MovieEvent(MovieUri movieUri, DateTimeOffset? timestamp = null)
    {
        MovieUri = movieUri;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }

    public string EventStreamId => MovieUri.Uri;

    public abstract void Apply(MovieReadModel model);

    [EventDescription]
    public abstract string? GetDescription();
}
