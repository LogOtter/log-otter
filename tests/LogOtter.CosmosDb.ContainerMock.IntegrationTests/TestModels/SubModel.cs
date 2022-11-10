using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;

public class SubModel
{
    public string Value { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TestEnum? NullableEnum { get; set; }
}