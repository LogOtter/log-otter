using System.Text.RegularExpressions;

namespace LogOtter.Obfuscate;

public static class Obfuscate
{
    private const string ObfuscationString = "****";
    private static readonly Regex EmailRegex = new(@"^(?<Username>[A-Z0-9._%+-]+)@(?<DomainName>[A-Z0-9.-]+\.[A-Z]{2,})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? Email(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return email;
        }

        var match = EmailRegex.Match(email);
        if (!match.Success)
        {
            return ObfuscationString;
        }

        var username = match.Groups["Username"].Value;
        var domainName = match.Groups["DomainName"].Value;

        var obfuscatedUsername = username.Length <= 5
            ? username[..1] + ObfuscationString
            : username[..2] + ObfuscationString + username[^1];

        return $"{obfuscatedUsername}@{domainName}";
    }

    public static string? Phone(string? phoneNumber)
    {
        if (phoneNumber == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        if (phoneNumber.Length < 11)
        {
            return ObfuscationString;
        }

        if (phoneNumber.StartsWith("+"))
        {
            return phoneNumber[..4] + ObfuscationString + phoneNumber[^2..];
        }

        return phoneNumber[..2] + ObfuscationString + phoneNumber[^2..];
    }

    public static string? Name(string? firstName, string? lastName)
    {
        if (firstName == null && lastName == null)
        {
            return null;
        }

        var obfuscatedFirstName = !string.IsNullOrEmpty(firstName)
            ? firstName.Length < 5
                ? firstName[..1] + ObfuscationString
                : firstName[..2] + ObfuscationString
            : string.Empty;

        var obfuscatedLastName = !string.IsNullOrEmpty(lastName)
            ? lastName.Length < 5
                ? lastName[..1] + ObfuscationString
                : lastName[..2] + ObfuscationString
            : string.Empty;

        return $"{obfuscatedFirstName} {obfuscatedLastName}".Trim();
    }
}