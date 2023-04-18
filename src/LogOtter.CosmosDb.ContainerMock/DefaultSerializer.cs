using System.Text;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LogOtter.CosmosDb.ContainerMock;

internal class DefaultSerializer : CosmosSerializer
{
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
    private readonly JsonSerializerSettings? _serializerSettings;

    /// <summary>
    /// Create a serializer that uses the JSON.net serializer
    /// </summary>
    /// <remarks>
    /// This is internal to reduce exposure of JSON.net types so
    /// it is easier to convert to System.Text.Json
    /// </remarks>
    internal DefaultSerializer(CosmosSerializationOptions? cosmosSerializerOptions)
    {
        if (cosmosSerializerOptions == null)
        {
            _serializerSettings = null;
            return;
        }

        JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
        {
            NullValueHandling = cosmosSerializerOptions.IgnoreNullValues ? NullValueHandling.Ignore : NullValueHandling.Include,
            Formatting = cosmosSerializerOptions.Indented ? Formatting.Indented : Formatting.None,
            ContractResolver =
                cosmosSerializerOptions.PropertyNamingPolicy == CosmosPropertyNamingPolicy.CamelCase
                    ? new CamelCasePropertyNamesContractResolver()
                    : null,
            MaxDepth = 64, // https://github.com/advisories/GHSA-5crp-9r3c-p9vr
        };

        _serializerSettings = jsonSerializerSettings;
    }

    /// <summary>
    /// Convert a Stream to the passed in type.
    /// </summary>
    /// <typeparam name="T">The type of object that should be deserialized</typeparam>
    /// <param name="stream">An open stream that is readable that contains JSON</param>
    /// <returns>The object representing the deserialized stream</returns>
    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            using var sr = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(sr);
            return GetSerializer().Deserialize<T>(jsonTextReader);
        }
    }

    /// <summary>
    /// Converts an object to a open readable stream
    /// </summary>
    /// <typeparam name="T">The type of object being serialized</typeparam>
    /// <param name="input">The object to be serialized</param>
    /// <returns>An open readable stream containing the JSON of the serialized object</returns>
    public override Stream ToStream<T>(T input)
    {
        var streamPayload = new MemoryStream();
        using var streamWriter = new StreamWriter(streamPayload, encoding: DefaultEncoding, bufferSize: 1024, leaveOpen: true);
        using var writer = new JsonTextWriter(streamWriter) { Formatting = Formatting.None };
        GetSerializer().Serialize(writer, input);
        writer.Flush();
        streamWriter.Flush();
        streamPayload.Position = 0;
        return streamPayload;
    }

    /// <summary>
    /// JsonSerializer has hit a race conditions with custom settings that cause null reference exception.
    /// To avoid the race condition a new JsonSerializer is created for each call
    /// </summary>
    private JsonSerializer GetSerializer() => JsonSerializer.Create(_serializerSettings);
}
