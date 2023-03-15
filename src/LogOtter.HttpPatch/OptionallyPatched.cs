using Newtonsoft.Json;

namespace LogOtter.HttpPatch;

public interface IOptionallyPatched
{
    bool IsIncludedInPatch { get; }

    internal object? Value { get; }
}

[JsonConverter(typeof(OptionallyPatchedJsonSerializer.NewtonsoftJsonConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(OptionallyPatchedJsonSerializer.SystemTextJsonFactory))]
public readonly record struct OptionallyPatched<T>(bool IsIncludedInPatch, T? Value = default) : IOptionallyPatched
{
    object? IOptionallyPatched.Value => Value;

    public static implicit operator OptionallyPatched<T>(T val)
    {
        return new OptionallyPatched<T>(true, val);
    }

    public override string? ToString()
    {
        return Value?.ToString();
    }

    public T GetValueIfIncludedOrDefault(T defaultValue)
    {
        return IsIncludedInPatch
            ? Value!
            : defaultValue;
    }
}
