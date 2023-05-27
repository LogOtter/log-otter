using Newtonsoft.Json;

namespace CustomerApi.NonEventSourcedData.CustomerInterests;

public record SearchableInterest([property: JsonProperty("id")] string Id, string Uri, string Name)
{
    [JsonProperty("partitionKey")]
    public string PartitionKey => StaticPartition;

    public const string StaticPartition = "searchable";
}
