using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.ContainerMock;

internal class StringSerializationHelper
{
    private readonly CosmosSerializer _serializer;

    public StringSerializationHelper(CosmosSerializationOptions cosmosClientOptions)
    {
        _serializer = new DefaultSerializer(cosmosClientOptions);
    }

    public string SerializeObject<T>(T toSerialize)
    {
        using var stream = _serializer.ToStream(toSerialize);
        using var stringReader = new StreamReader(stream);
        return stringReader.ReadToEnd();
    }

    public T DeserializeObject<T>(string toDeserialize)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, leaveOpen: true);
        writer.Write(toDeserialize);
        writer.Flush();
        stream.Position = 0;
        var deserializeObject = _serializer.FromStream<T>(stream);
        return deserializeObject;
    }
}
