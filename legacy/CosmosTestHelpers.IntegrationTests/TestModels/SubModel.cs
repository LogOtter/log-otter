using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CosmosTestHelpers.IntegrationTests.TestModels;

public class SubModel
{
    public string Value { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TestEnum? NullableEnum { get; set; }
}