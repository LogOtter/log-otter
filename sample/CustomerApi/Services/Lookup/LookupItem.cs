using Newtonsoft.Json;

namespace CustomerApi.Services.Lookup;

public abstract record LookupItem([property:JsonProperty("id")]string Id,
    string Name,
    [property:JsonProperty("partitionKey")]string PartitionKey);
