using Newtonsoft.Json;

namespace CustomerApi.Services.CustomerInterests;

public abstract record CustomerInterest(
    [property: JsonProperty("id")] string Id,
    string Name,
    [property: JsonProperty("partitionKey")] string PartitionKey
);
