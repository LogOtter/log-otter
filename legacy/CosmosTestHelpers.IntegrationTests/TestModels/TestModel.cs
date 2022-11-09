using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CosmosTestHelpers.IntegrationTests.TestModels;

public class TestModel
{
    [JsonProperty("id")]
    public string Id { get; set; }

    public string Name { get; set; }

    public bool Value { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TestEnum EnumValue { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TestEnum? NullableEnum { get; set; }

    public TestEnum? NullableEnumNotString { get; set; }

    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; }

    public IEnumerable<SubModel> Children { get; set; }

    public SubModel OnlyChild { get; set; }

    public bool GetBoolValue()
    {
        return Value;
    }
}