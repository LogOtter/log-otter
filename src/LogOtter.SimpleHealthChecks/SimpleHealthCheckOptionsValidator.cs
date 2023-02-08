using Microsoft.Extensions.Options;

namespace LogOtter.SimpleHealthChecks;

internal class SimpleHealthCheckOptionsValidator : IValidateOptions<SimpleHealthCheckOptions>
{
    public ValidateOptionsResult Validate(string? name, SimpleHealthCheckOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Hostname))
        {
            return ValidateOptionsResult.Fail("Hostname is required");
        }

        if (options.Port is < 1 or > 65535)
        {
            return ValidateOptionsResult.Fail("Port must be between 1-65535");
        }

        if (string.IsNullOrWhiteSpace(options.UrlPath))
        {
            return ValidateOptionsResult.Fail("UrlPath is required");
        }

        if (!options.UrlPath.StartsWith("/"))
        {
            return ValidateOptionsResult.Fail("UrlPath should start with /");
        }

        if (options.Scheme != "http" && options.Scheme != "https")
        {
            return ValidateOptionsResult.Fail("Scheme must be 'http' or 'https'");
        }

        return ValidateOptionsResult.Success;
    }
}