using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.SimpleHealthChecks;

public static class ConfigureExtensions
{
    public static ISimpleHealthChecksBuilder AddSimpleHealthChecks(
        this IServiceCollection services,
        Action<SimpleHealthCheckHostOptions>? configure = null
    )
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddHostedService<SimpleHealthCheckService>();

        return new SimpleHealthChecksBuilder(services);
    }
}