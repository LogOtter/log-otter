using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverter;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace LogOtter.HttpPatch;

internal static class OptionallyPatchedJsonSerializer
{
    internal class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<IOptionallyPatched>
    {
        public override void WriteJson(JsonWriter writer, IOptionallyPatched? value, JsonSerializer serializer)
        {
            if (value is { IsIncludedInPatch: true })
            {
                serializer.Serialize(writer, value.Value);
            }
            else
            {
                writer.WriteUndefined();
            }
        }

        public override IOptionallyPatched ReadJson(
            JsonReader reader,
            Type objectType,
            IOptionallyPatched? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            var underlyingType = objectType.GetGenericArguments().Single();

            if (reader.TokenType == JsonToken.Undefined)
            {
                return (IOptionallyPatched)Activator.CreateInstance(objectType, false, default)!;
            }

            var deserializedItem = serializer.Deserialize(reader, underlyingType);
            return (IOptionallyPatched)Activator.CreateInstance(objectType, true, deserializedItem)!;
        }
    }

    private class SystemTextJson<T> : System.Text.Json.Serialization.JsonConverter<OptionallyPatched<T>>
    {
        public override void Write(Utf8JsonWriter writer, OptionallyPatched<T> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override OptionallyPatched<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var underlyingType = typeToConvert.GetGenericArguments().Single();
            var deserializedItem = System.Text.Json.JsonSerializer.Deserialize(ref reader, underlyingType, options);
            var optionallyPatchedWrapper = Activator.CreateInstance(typeToConvert, true, deserializedItem);
            return (OptionallyPatched<T>)optionallyPatchedWrapper!;
        }
    }

    internal class SystemTextJsonFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(OptionallyPatched<>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var underlyingType = typeToConvert.GetGenericArguments().Single();
            var genericType = typeof(SystemTextJson<>).MakeGenericType(underlyingType);
            return (JsonConverter)Activator.CreateInstance(genericType)!;
        }
    }
}
