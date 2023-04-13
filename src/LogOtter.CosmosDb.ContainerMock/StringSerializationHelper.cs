using System.Reflection;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.ContainerMock;

internal class StringSerializationHelper
{
    private const string DefaultSerializerName = "Microsoft.Azure.Cosmos.CosmosJsonDotNetSerializer";
    private readonly CosmosClientOptions _cosmosClientOptions;
    private readonly CosmosSerializer _serializer;

    public StringSerializationHelper(CosmosClientOptions cosmosClientOptions)
    {
        _cosmosClientOptions = cosmosClientOptions;
        _serializer = _cosmosClientOptions.Serializer ?? CreateDefaultSerializer();
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

    /// <summary>
    /// Need to fall back to the default, so that we can simulate the behaviour of Cosmos.
    /// This class is internal so using reflection to create it.
    /// </summary>
    /// <returns></returns>
    private CosmosSerializer CreateDefaultSerializer()
    {
        var defaultSerializerType = typeof(CosmosSerializer).Assembly.GetType(DefaultSerializerName);
        if (defaultSerializerType == null)
        {
            throw new NullReferenceException($"Could not locate type {DefaultSerializerName} to use as the default serializer");
        }

        var ctor =
            defaultSerializerType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance,
                new[] { typeof(CosmosSerializationOptions) }
            )
            ?? throw new NullReferenceException($"Could not locate constructor on {DefaultSerializerName} which accepts only serialization options");

        var serializer =
            ctor.Invoke(new object[] { _cosmosClientOptions.SerializerOptions })
            ?? throw new NullReferenceException($"Failed to construct type {DefaultSerializerName} to use as the default serializer");

        return (CosmosSerializer)serializer;
    }
}
