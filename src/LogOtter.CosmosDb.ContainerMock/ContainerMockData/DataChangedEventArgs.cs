using Newtonsoft.Json;

namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

public class DataChangedEventArgs : EventArgs
{
    public string Json { get; }

    public Operation Operation { get; set; }

    internal DataChangedEventArgs(Operation operation, string json)
    {
        Operation = operation;
        Json = json;
    }

    public T Deserialize<T>()
    {
        return JsonConvert.DeserializeObject<T>(Json);
    }
}

public enum Operation
{
    Created,
    Updated,
    Deleted
}