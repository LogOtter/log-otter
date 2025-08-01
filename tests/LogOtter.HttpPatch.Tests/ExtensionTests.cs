using System.Text.Json;
using LogOtter.HttpPatch.Tests.Api;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace LogOtter.HttpPatch.Tests;

public class ExtensionTests
{
    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public void ReturnsFalseIfNotPatched(SerializationEngine engine)
    {
        var deserialized = DeserializeFromStringWith<TestRequest>(engine, "{}");

        const string primitiveValue = "primitive";
        var addressValue = new Address("Alpha Tower", "B1 1TT");

        deserialized.Primitive.IsIncludedInPatchAndDifferentFrom(primitiveValue).ShouldBeFalse();
        deserialized.Address.IsIncludedInPatchAndDifferentFrom(addressValue).ShouldBeFalse();
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public void ReturnsTrueIfDifferent(SerializationEngine engine)
    {
        var deserialized = DeserializeFromStringWith<TestRequest>(
            engine,
            "{ \"primitive\": \"hello world\", \"address\": { \"line1\": \"Centenary Plaza\", \"postcode\": \"B1 1TB\" } }"
        );

        const string primitiveValue = "primitive";
        var addressValue = new Address("Alpha Tower", "B1 1TT");

        deserialized.Primitive.IsIncludedInPatchAndDifferentFrom(primitiveValue).ShouldBeTrue();
        deserialized.Address.IsIncludedInPatchAndDifferentFrom(addressValue).ShouldBeTrue();
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public void ReturnsTrueIfSame(SerializationEngine engine)
    {
        var deserialized = DeserializeFromStringWith<TestRequest>(
            engine,
            "{ \"primitive\": \"primitive\", \"address\": { \"line1\": \"Alpha Tower\", \"postcode\": \"B1 1TT\" } }"
        );

        const string primitiveValue = "primitive";
        var addressValue = new Address("Alpha Tower", "B1 1TT");

        deserialized.Primitive.IsIncludedInPatchAndDifferentFrom(primitiveValue).ShouldBeFalse();
        deserialized.Address.IsIncludedInPatchAndDifferentFrom(addressValue).ShouldBeFalse();
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public void ReturnsFalseIfPrimitiveSameWithComparer(SerializationEngine engine)
    {
        var deserialized = DeserializeFromStringWith<TestRequest>(engine, "{ \"primitive\": \"PRIMITIVE\" }");

        const string primitiveValue = "primitive";

        deserialized.Primitive.IsIncludedInPatchAndDifferentFrom(primitiveValue, StringComparer.OrdinalIgnoreCase).ShouldBeFalse();
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public void ReturnsFalseIfSameWithComparer(SerializationEngine engine)
    {
        var deserialized = DeserializeFromStringWith<TestRequest>(
            engine,
            "{ \"address\": { \"line1\": \"Alpha Tower\", \"postcode\": \"b1 1tt\" } }"
        );

        var addressValue = new Address("Alpha Tower", "B1 1TT");

        deserialized.Address.IsIncludedInPatchAndDifferentFrom(addressValue, new AddressEqualityComparer()).ShouldBeFalse();
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

    private record Address(string Line1, string Postcode);

    private record TestRequest(OptionallyPatched<string> Primitive = default, OptionallyPatched<Address> Address = default);

    private class AddressEqualityComparer : IEqualityComparer<Address>
    {
        public bool Equals(Address? x, Address? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Line1 == y.Line1 && string.Equals(x.Postcode, y.Postcode, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(Address obj)
        {
            return HashCode.Combine(obj.Line1, obj.Postcode);
        }
    }
}
