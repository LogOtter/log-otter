namespace CustomerApi.Tests;

public readonly struct Discretionary<T> : IEquatable<Discretionary<T>>
{
    public bool IsSpecified { get; }
    public T Value { get; }

    private Discretionary(T value)
    {
        IsSpecified = true;
        Value = value;
    }

    public T GetValueOrDefault(T defaultValue)
    {
        return IsSpecified ? Value : defaultValue;
    }

    public bool Equals(Discretionary<T> other)
    {
        return EqualityComparer<T?>.Default.Equals(Value, other.Value) && IsSpecified == other.IsSpecified;
    }

    public override bool Equals(object? obj)
    {
        return obj is Discretionary<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, IsSpecified);
    }

    public static bool operator ==(Discretionary<T> left, Discretionary<T> right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(Discretionary<T> left, Discretionary<T> right)
    {
        return !left.Equals(right);
    }
}