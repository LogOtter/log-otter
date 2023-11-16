using System.Text.RegularExpressions;

namespace CustomerApi.Uris;

public class CustomerUri(string customerId) : IEquatable<CustomerUri>
{
    private static readonly Regex CustomerUriRegex = new("^/customers/(?<CustomerId>[0-9A-Za-z]+)$");

    public string CustomerId { get; } = new Id(customerId);

    public string Uri => $"/customers/{CustomerId}";

    public bool Equals(CustomerUri? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return CustomerId.Equals(other.CustomerId);
    }

    public override string ToString()
    {
        return Uri;
    }

    public static CustomerUri Parse(string customerUri)
    {
        var match = CustomerUriRegex.Match(customerUri);

        if (!match.Success)
        {
            throw new ArgumentException($"Invalid customer URI: {customerUri}", nameof(customerUri));
        }

        return new CustomerUri(match.Groups["CustomerId"].Value);
    }

    public static CustomerUri Generate()
    {
        return new CustomerUri(Id.Generate());
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

        return Equals((CustomerUri)obj);
    }

    public static bool operator ==(CustomerUri left, CustomerUri right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CustomerUri left, CustomerUri right)
    {
        return !left.Equals(right);
    }

    public override int GetHashCode()
    {
        return CustomerId.GetHashCode();
    }
}
