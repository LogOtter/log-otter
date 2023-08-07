using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Newtonsoft.Json;

namespace CustomerApi.Events.Movies;

#pragma warning disable CS8618

public class MovieReadModel : ISnapshot
{
    public const string StaticPartitionKey = "movies";

    public string Name { get; set; }

    public MovieUri MovieUri { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    [JsonProperty("id")]
    public string Id { get; init; }

    [JsonProperty("partitionKey")]
    public string PartitionKey => StaticPartitionKey;

    public DateTimeOffset? DeletedAt { get; set; }

    public int Revision { get; set; }
}
