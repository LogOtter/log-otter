using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb;

internal class CosmosDbOptionsValidator : IValidateOptions<CosmosDbOptions>
{
    public ValidateOptionsResult Validate(string? name, CosmosDbOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("ConnectionString not specified");
        }

        if (string.IsNullOrWhiteSpace(options.DatabaseId))
        {
            return ValidateOptionsResult.Fail("DatabaseId not specified");
        }

        return ValidateOptionsResult.Success;
    }
}