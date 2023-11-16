using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Movies;

public abstract class MovieEvent(MovieUri movieUri, DateTimeOffset? timestamp = null) : IEvent<MovieReadModel>
{
    public MovieUri MovieUri { get; } = movieUri;

    public DateTimeOffset Timestamp { get; } = timestamp ?? DateTimeOffset.UtcNow;

    public string EventStreamId => MovieUri.Uri;

    public abstract void Apply(MovieReadModel model);

    [EventDescription]
    public abstract string? GetDescription();
}
