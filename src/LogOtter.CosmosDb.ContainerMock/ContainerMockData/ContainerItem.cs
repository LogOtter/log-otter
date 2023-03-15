using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

internal class ContainerItem
{
    public PartitionKey PartitionKey { get; }
    public string Id { get; }
    public string Json { get; }
    public string ETag { get; private set; }
    public int? ExpiryTime { get; }

    public bool RequireETagOnNextUpdate { get; private set; }
    public bool HasScheduledETagMismatch { get; private set; }

    public ContainerItem(string id, string json, PartitionKey partitionKey, int? expiryTime)
    {
        Id = id;
        ExpiryTime = expiryTime;
        Json = json;
        PartitionKey = partitionKey;
        ETag = GenerateETag();
    }

    internal ContainerItem(
        string id,
        string json,
        PartitionKey partitionKey,
        int? expiryTime,
        string eTag,
        bool requireETagOnNextUpdate,
        bool hasScheduledETagMismatch)
    {
        Id = id;
        ExpiryTime = expiryTime;
        Json = json;
        PartitionKey = partitionKey;
        ETag = eTag;
        RequireETagOnNextUpdate = requireETagOnNextUpdate;
        HasScheduledETagMismatch = hasScheduledETagMismatch;
    }

    public T Deserialize<T>()
    {
        return JsonConvert.DeserializeObject<T>(Json);
    }

    public void ScheduleMismatchETagOnNextUpdate()
    {
        RequireETagOnNextUpdate = true;
        HasScheduledETagMismatch = true;
    }

    public void ChangeETag()
    {
        ETag = GenerateETag();
        HasScheduledETagMismatch = false;
    }

    private static string GenerateETag()
    {
        return $"\"{Guid.NewGuid()}\"";
    }
}
