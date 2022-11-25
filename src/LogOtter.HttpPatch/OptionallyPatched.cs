namespace LogOtter.HttpPatch;

public interface IOptionallyPatched
{
    bool IsIncludedInPatch { get; }
    
    internal object? Value { get; }
}

[Newtonsoft.Json.JsonConverter(typeof(OptionallyPatchedJsonSerializer.NewtonsoftJsonConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(OptionallyPatchedJsonSerializer.SystemTextJsonFactory))]
public readonly record struct OptionallyPatched<T>(bool IsIncludedInPatch, T? Value = default) : IOptionallyPatched
{
    public static implicit operator OptionallyPatched<T>(T val) => new(true, val);

    object? IOptionallyPatched.Value => Value;

    public override string? ToString() => Value?.ToString();

    public T GetValueIfIncludedOrDefault(T defaultValue) => IsIncludedInPatch ? Value! : defaultValue;
}