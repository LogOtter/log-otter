using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class Message : IEventualSnapshot
{
    [JsonProperty("id")]
    public string Id { get; init; }

    public DateTimeOffset LastUpdated { get; set; }

    [JsonProperty("partitionKey")]
    public string PartitionKey => Id;

    public DateTimeOffset? DeletedAt { get; set; }

    public string State { get; set; }
}
