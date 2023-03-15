using Microsoft.Extensions.Options;

namespace LogOtter.SimpleHealthChecks;

internal class SimpleHealthCheckOptionsValidator : IValidateOptions<SimpleHealthCheckHostOptions>
{
    public ValidateOptionsResult Validate(string? name, SimpleHealthCheckHostOptions hostOptions)
    {
        if (string.IsNullOrWhiteSpace(hostOptions.Hostname))
        {
            return ValidateOptionsResult.Fail("Hostname is required");
        }

        if (hostOptions.Port is < 1 or > 65535)
        {
            return ValidateOptionsResult.Fail("Port must be between 1-65535");
        }

        if (hostOptions.Scheme != "http" && hostOptions.Scheme != "https")
        {
            return ValidateOptionsResult.Fail("Scheme must be 'http' or 'https'");
        }

        return ValidateOptionsResult.Success;
    }
}
