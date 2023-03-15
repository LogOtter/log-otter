using System.Diagnostics.CodeAnalysis;

namespace LogOtter.SimpleHealthChecks;

internal class PathString : IEquatable<PathString>
{
    public static PathString Root => new("/");

    public string? Value { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue => !string.IsNullOrEmpty(Value);

    public PathString(string? value)
    {
        if (!string.IsNullOrEmpty(value) && value[0] != '/')
        {
            throw new ArgumentException("Path must start with a slash", nameof(value));
        }

        Value = value;
    }

    public bool Equals(PathString? other)
    {
        if (other == null)
        {
            return !HasValue;
        }

        return Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    public bool StartsWithSegments(PathString other, out PathString remaining)
    {
        var value1 = Value ?? string.Empty;
        var value2 = other.Value ?? string.Empty;
        if (value1.StartsWith(value2, StringComparison.OrdinalIgnoreCase))
        {
            if (value1.Length == value2.Length || value1[value2.Length] == '/')
            {
                remaining = new PathString(value1[value2.Length..]);
                return true;
            }
        }

        remaining = new PathString(string.Empty);
        return false;
    }

    public bool Equals(PathString other, StringComparison comparisonType)
    {
        if (!HasValue && !other.HasValue)
        {
            return true;
        }

        return string.Equals(Value, other.Value, comparisonType);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return !HasValue;
        }

        return obj is PathString pathString && Equals(pathString);
    }

    public override int GetHashCode()
    {
        return HasValue
            ? StringComparer.OrdinalIgnoreCase.GetHashCode(Value)
            : 0;
    }
}
