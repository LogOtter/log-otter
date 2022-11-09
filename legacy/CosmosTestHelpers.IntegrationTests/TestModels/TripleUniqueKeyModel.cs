using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CosmosTestHelpers.IntegrationTests.TestModels;

public class TripleUniqueKeyModel
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    [JsonProperty("partitionKey")]
    public string PartitionKey => CustomerId;

    public string CustomerId { get; set; }

    public string ItemId { get; set; }

    public string Value { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TestEnum Type { get; set; }
}