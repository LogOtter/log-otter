using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LogOtter.SimpleHealthChecks;

public static class ConfigureExtensions
{
    public static IServiceCollection AddSimpleHealthChecks(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<SimpleHealthCheckOptions>, SimpleHealthCheckOptionsValidator>();
        services.AddSingleton<IHostedService, SimpleHealthCheckService>();

        return services;
    }
}