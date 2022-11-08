using System.Text.RegularExpressions;

namespace CustomerApi.Uris;

public class Id : IEquatable<Id>
{
    private static readonly Regex IdRegex = new Regex("^[0-9A-Za-z]+$");

    private readonly string _id;

    public Id(string id)
    {
        if (!IdRegex.IsMatch(id))
        {
            throw new ArgumentException("Invalid ID", nameof(id));
        }

        _id = id;
    }

    public static implicit operator string(Id id) => id._id;

    public override string ToString() => _id;

    public static Id Generate()
    {
        var id = Guid.NewGuid().ToShortString();
        return new Id(id);
    }

    public static bool TryParse(string s, out Id result)
    {
        try
        {
            result = new Id(s);
            return true;
        }
        catch (ArgumentException)
        {
            result = null!;
            return false;
        }
    }

    public bool Equals(Id? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _id == other._id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((Id)obj);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }
}