using Newtonsoft.Json;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;

public class PascalCaseIdModel
{
    public string? Id { get; set; }

    [JsonProperty("partitionKey")]
    public string? PartitionKey { get; set; }
}
