using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore.Tests.TestEvents;

public class TestEventProjection : ISnapshot
{
    private const string StaticPartitionKey = "/test";

    public int Revision { get; set; }

    [JsonProperty("id")]
    public required string Id { get; init; }

    [JsonProperty("partitionKey")]
    public string PartitionKey => StaticPartitionKey;
    public DateTimeOffset? DeletedAt { get; set; }
    public string Name { get; set; } = "";
}
