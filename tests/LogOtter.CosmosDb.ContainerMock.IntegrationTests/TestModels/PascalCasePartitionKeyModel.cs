using Newtonsoft.Json;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;

public class PascalCasePartitionKeyModel
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    public string? PartitionKey { get; set; }
}
