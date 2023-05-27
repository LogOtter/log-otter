using Newtonsoft.Json;

namespace CustomerApi.NonEventSourcedData.CustomerInterests;

public record CustomerInterest(
    [property: JsonProperty("id")] string Id,
    string Uri,
    string Name,
    [property: JsonProperty("partitionKey")] string PartitionKey
);
