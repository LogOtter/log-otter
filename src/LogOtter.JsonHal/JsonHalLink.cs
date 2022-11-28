namespace LogOtter.JsonHal;

public sealed class JsonHalLink: IEquatable<JsonHalLink>
{
    public string Type { get; }
    public string Href { get; }

    public JsonHalLink(string type, string href)
    {
        Type = type;
        Href = href;
    }

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
        return ReferenceEquals(this, obj) || obj is JsonHalLink other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Href);
    }
}