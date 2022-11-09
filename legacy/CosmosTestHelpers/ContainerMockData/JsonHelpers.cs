using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace CosmosTestHelpers.ContainerMockData;

internal static class JsonHelpers
{
    public static string GetValueFromJson(string json, string path)
    {
        var jObject = JObject.Parse(json);
        var jPath = path.Substring(1).Replace('/', '.');
        var jTokenProperty = jObject.SelectToken(jPath);

        if (jTokenProperty == null)
        {
            throw new ArgumentException($"Could not extract property key ({path}) from json: Path not found.");
        }

        if (jTokenProperty.Type != JTokenType.String && jTokenProperty.Type != JTokenType.Null)
        {
            throw new ArgumentException($"Could not extract property key ({path}) from json. Only string or null values supported.");
        }

        return jTokenProperty.Type == JTokenType.Null 
            ? null 
            : jTokenProperty.Value<string>();
    }

    public static string GetIdFromJson(string json)
    {
        return GetValueFromJson(json, "/id");
    }

    public static int GetTtlFromJson(string json, int defaultDocumentTimeToLive)
    {
        var ttl = (int?)null;

        var jObject = JObject.Parse(json);
        var jTokenProperty = jObject.SelectToken("ttl");

        if (jTokenProperty != null)
        {
            if (jTokenProperty.Type != JTokenType.Integer && jTokenProperty.Type != JTokenType.Null)
            {
                //TODO: What does real Cosmos do in this situation?
                throw new ArgumentException($"Could not extract ttl from json. Only int or null values supported.", nameof(json));
            }

            ttl = jTokenProperty.Type == JTokenType.Null ? (int?)null : jTokenProperty.Value<int>();
        }

        return ttl ?? defaultDocumentTimeToLive;
    }

    public static ISet<(string path, string value)> GetUniqueKeyValueFromJson(string json, UniqueKey uniqueKey)
    {
        var uniqueKeyValue = new HashSet<(string, string)>();
        var jObject = JObject.Parse(json);

        foreach (var path in uniqueKey.Paths)
        {
            var jPath = path.Substring(1).Replace('/', '.');
            var property = jObject.SelectToken(jPath);

            if (property == null)
            {
                throw new InvalidOperationException("Unique key not present");
            }

            var pathValue = property.Value<string>();

            if (pathValue == null)
            {
                throw new InvalidOperationException("Unique key is null");
            }

            uniqueKeyValue.Add((path, pathValue));
        }

        return uniqueKeyValue;
    }
}