using Microsoft.Azure.Cosmos;

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

    private readonly StringSerializationHelper _serializationHelper;

    public ContainerItem(string id, string json, PartitionKey partitionKey, int? expiryTime, StringSerializationHelper serializationHelper)
    {
        Id = id;
        ExpiryTime = expiryTime;
        _serializationHelper = serializationHelper;
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
        bool hasScheduledETagMismatch,
        StringSerializationHelper serializationHelper
    )
    {
        Id = id;
        ExpiryTime = expiryTime;
        _serializationHelper = serializationHelper;
        Json = json;
        PartitionKey = partitionKey;
        ETag = eTag;
        RequireETagOnNextUpdate = requireETagOnNextUpdate;
        HasScheduledETagMismatch = hasScheduledETagMismatch;
    }

    public T Deserialize<T>()
    {
        return _serializationHelper.DeserializeObject<T>(Json);
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
