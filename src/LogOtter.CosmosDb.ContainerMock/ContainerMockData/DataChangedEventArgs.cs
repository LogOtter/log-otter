using Newtonsoft.Json;

namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

public class DataChangedEventArgs : EventArgs
{
    private readonly StringSerializationHelper _serializationHelper;

    public string Json { get; }

    public Operation Operation { get; set; }

    internal DataChangedEventArgs(Operation operation, string json, StringSerializationHelper serializationHelper)
    {
        Operation = operation;
        Json = json;
        _serializationHelper = serializationHelper;
    }

    public T Deserialize<T>()
    {
        return _serializationHelper.DeserializeObject<T>(Json);
    }
}

public enum Operation
{
    Created,
    Updated,
    Deleted
}
