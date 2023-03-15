using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogOtter.JsonHal;

public class JsonHalLinkCollectionConverter : JsonConverter<JsonHalLinkCollection>
{
    public override void Write(Utf8JsonWriter writer, JsonHalLinkCollection value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var results in value.GroupBy(l => l.Type))
        {
            var count = results.Count();

            writer.WritePropertyName(results.Key);

            if (count > 1)
            {
                writer.WriteStartArray();
            }

            foreach (var link in results)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("href");
                writer.WriteStringValue(link.Href);
                writer.WriteEndObject();
            }

            if (count > 1)
            {
                writer.WriteEndArray();
            }
        }

        writer.WriteEndObject();
    }

    public override JsonHalLinkCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var linkCollection = new JsonHalLinkCollection();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return linkCollection;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var linkType = reader.GetString();

                if (linkType == null)
                {
                    throw new JsonException();
                }

                reader.Read();

                var hrefs = ReadLinkHrefs(ref reader);

                foreach (var href in hrefs)
                {
                    linkCollection.AddLink(linkType, href);
                }
            }
        }

        throw new JsonException();
    }

    private static IList<string> ReadLinkHrefs(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            return ReadLinkHrefsArray(ref reader);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var href = ReadLinkHref(ref reader);

            return href == null
                ? new List<string>()
                : new List<string> { href };
        }

        throw new JsonException();
    }

    private static IList<string> ReadLinkHrefsArray(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        var hrefs = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return hrefs;
            }

            var href = ReadLinkHref(ref reader);

            if (href != null)
            {
                hrefs.Add(href);
            }
        }

        throw new JsonException();
    }

    private static string? ReadLinkHref(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException();
        }

        var hrefProperty = reader.GetString();
        if (hrefProperty != "href")
        {
            throw new JsonException();
        }

        reader.Read();
        var href = reader.GetString();

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndObject)
        {
            throw new JsonException();
        }

        return href;
    }
}
