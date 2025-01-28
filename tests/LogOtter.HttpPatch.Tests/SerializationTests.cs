using System.Text.Json;
using LogOtter.HttpPatch.Tests.Api;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable NotAccessedPositionalProperty.Local

namespace LogOtter.HttpPatch.Tests;

public class SerializationTests
{
    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public void MissingValueIsShownAsNotInPatch(SerializationEngine engine)
    {
        var deserialized = DeserializeFromStringWith<TestRequest>(engine, "{}");

        deserialized.Primitive.IsIncludedInPatch.ShouldBeFalse();
        deserialized.Address.IsIncludedInPatch.ShouldBeFalse();
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    public void UndefinedIsShownAsNotInPatch(SerializationEngine engine)
    {
        var deserialized = DeserializeFromStringWith<TestRequest>(engine, "{\"primitive\": undefined}");

        deserialized.Primitive.IsIncludedInPatch.ShouldBeFalse();
        deserialized.Address.IsIncludedInPatch.ShouldBeFalse();
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public void DeserializesWithValue(SerializationEngine engine)
    {
        var deserialized = DeserializeFromStringWith<TestRequest>(
            engine,
            "{ \"primitive\": \"hello world\", \"address\": { \"line1\": \"Alpha Tower\", \"postcode\": \"B1 1TT\" } }"
        );

        deserialized.Primitive.IsIncludedInPatch.ShouldBeTrue();
        deserialized.Primitive.Value.ShouldBe("hello world");

        deserialized.Address.IsIncludedInPatch.ShouldBeTrue();
        deserialized.Address.Value.ShouldNotBeNull();
        deserialized.Address.Value.Line1.ShouldBe("Alpha Tower");
        deserialized.Address.Value.Postcode.ShouldBe("B1 1TT");
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    public void RoundTripSerializeWhenIncludedInPatch(SerializationEngine engine)
    {
        var serialized = SerializeWith(engine, new TestRequest("hello world", new Address("Alpha Tower", "B1 1TT")));

        var deserialized = DeserializeFromStringWith<TestRequest>(engine, serialized);
        deserialized.Primitive.IsIncludedInPatch.ShouldBeTrue();
        deserialized.Primitive.Value.ShouldBe("hello world");

        deserialized.Address.IsIncludedInPatch.ShouldBeTrue();
        deserialized.Address.Value.ShouldNotBeNull();
        deserialized.Address.Value.Line1.ShouldBe("Alpha Tower");
        deserialized.Address.Value.Postcode.ShouldBe("B1 1TT");
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    public void RoundTripSerializeWhenNotIncludedInPatch(SerializationEngine engine)
    {
        var serialized = SerializeWith(engine, new TestRequest());

        var deserialized = DeserializeFromStringWith<TestRequest>(engine, serialized);
        deserialized.Address.IsIncludedInPatch.ShouldBeFalse();
        deserialized.Primitive.IsIncludedInPatch.ShouldBeFalse();
    }

    private static T DeserializeFromStringWith<T>(SerializationEngine engine, string toDeserialize)
    {
        switch (engine)
        {
            case SerializationEngine.Newtonsoft:
                return JsonConvert.DeserializeObject<T>(toDeserialize) ?? throw new InvalidOperationException("Got null from deserialize call");
            case SerializationEngine.SystemText:
                // Turning on case insensitivity as that is true in the defaults in asp.net APIs
                return JsonSerializer.Deserialize<T>(toDeserialize, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("Got null from deserialize call");
            default:
                throw new ArgumentOutOfRangeException(nameof(engine), engine, null);
        }
    }

    private static string SerializeWith(SerializationEngine engine, object toSerialize)
    {
        switch (engine)
        {
            case SerializationEngine.Newtonsoft:
                return JsonConvert.SerializeObject(toSerialize) ?? throw new InvalidOperationException("Got null from deserialize call");
            case SerializationEngine.SystemText:
                return JsonSerializer.Serialize(toSerialize) ?? throw new InvalidOperationException("Got null from deserialize call");
            default:
                throw new ArgumentOutOfRangeException(nameof(engine), engine, null);
        }
    }

    private record Address(string Line1, string Postcode);

    private record TestRequest(OptionallyPatched<string> Primitive = default, OptionallyPatched<Address> Address = default);
}
