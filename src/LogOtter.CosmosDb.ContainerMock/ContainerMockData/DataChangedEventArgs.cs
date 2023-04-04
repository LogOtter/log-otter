using Newtonsoft.Json;

namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

public class DataChangedEventArgs : EventArgs
{
    private readonly JsonSerializerSettings? _jsonSerializerSettings;
    public string Json { get; }

    public Operation Operation { get; }

    internal DataChangedEventArgs(Operation operation, string json, JsonSerializerSettings? jsonSerializerSettings)
    {
        _jsonSerializerSettings = jsonSerializerSettings;
        Operation = operation;
        Json = json;
    }

    public T Deserialize<T>()
    {
        return JsonConvert.DeserializeObject<T>(Json, _jsonSerializerSettings);
    }
}

public enum Operation
{
    Created,
    Updated,
    Deleted
}
