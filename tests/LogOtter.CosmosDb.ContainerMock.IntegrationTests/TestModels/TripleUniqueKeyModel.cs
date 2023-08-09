using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;

public class TripleUniqueKeyModel
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    [JsonProperty("partitionKey")]
    public string PartitionKey => CustomerId;

    public required string CustomerId { get; set; }

    public required string ItemId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TestEnum Type { get; set; }
}
