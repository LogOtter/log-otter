using System.Text.RegularExpressions;

namespace CustomerApi.Uris;

public class SongUri : IEquatable<SongUri>
{
    private static readonly Regex SongUriRegex = new("^/songs/(?<SongId>[0-9A-Za-z]+)$");

    public string SongId { get; }

    public string Uri => $"/songs/{SongId}";

    public SongUri(string songId)
    {
        SongId = new Id(songId);
    }

    public bool Equals(SongUri? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return SongId.Equals(other.SongId);
    }

    public override string ToString()
    {
        return Uri;
    }

    public static SongUri Parse(string songUri)
    {
        var match = SongUriRegex.Match(songUri);

        if (!match.Success)
        {
            throw new ArgumentException($"Invalid song URI: {songUri}", nameof(songUri));
        }

        return new SongUri(match.Groups["SongId"].Value);
    }

    public static SongUri Generate()
    {
        return new SongUri(Id.Generate());
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

        return Equals((SongUri)obj);
    }

    public static bool operator ==(SongUri left, SongUri right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SongUri left, SongUri right)
    {
        return !left.Equals(right);
    }

    public override int GetHashCode()
    {
        return SongId.GetHashCode();
    }
}
