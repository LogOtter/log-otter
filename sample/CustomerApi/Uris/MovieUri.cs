using System.Text.RegularExpressions;

namespace CustomerApi.Uris;

public class MovieUri(string movieId) : IEquatable<MovieUri>
{
    private static readonly Regex MovieUriRegex = new("^/movies/(?<MovieId>[0-9A-Za-z]+)$");

    public string MovieId { get; } = new Id(movieId);

    public string Uri => $"/movies/{MovieId}";

    public bool Equals(MovieUri? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return MovieId.Equals(other.MovieId);
    }

    public override string ToString()
    {
        return Uri;
    }

    public static MovieUri Parse(string movieUri)
    {
        var match = MovieUriRegex.Match(movieUri);

        if (!match.Success)
        {
            throw new ArgumentException($"Invalid movie URI: {movieUri}", nameof(movieUri));
        }

        return new MovieUri(match.Groups["MovieId"].Value);
    }

    public static MovieUri Generate()
    {
        return new MovieUri(Id.Generate());
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

        return Equals((MovieUri)obj);
    }

    public static bool operator ==(MovieUri left, MovieUri right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MovieUri left, MovieUri right)
    {
        return !left.Equals(right);
    }

    public override int GetHashCode()
    {
        return MovieId.GetHashCode();
    }
}
