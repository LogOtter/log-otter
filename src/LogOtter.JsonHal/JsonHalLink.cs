namespace LogOtter.JsonHal;

public sealed class JsonHalLink(string type, string href) : IEquatable<JsonHalLink>
{
    public string Type { get; } = type;
    public string Href { get; } = href;

    public bool Equals(JsonHalLink? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Type == other.Type && Href == other.Href;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is JsonHalLink other && Equals(other));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Href);
    }
}
