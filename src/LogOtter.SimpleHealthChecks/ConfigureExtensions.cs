using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        services.AddSingleton<IHostedService, SimpleHealthCheckService>();

        return new SimpleHealthChecksBuilder(services);
    }
}