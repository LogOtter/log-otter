using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.SimpleHealthChecks;

internal class SimpleHealthChecksBuilder(IServiceCollection services) : ISimpleHealthChecksBuilder
{
    public ISimpleHealthChecksBuilder AddEndpoint(string path)
    {
        return AddEndpoint(new PathString(path), new SimpleHealthCheckOptions());
    }

    public ISimpleHealthChecksBuilder AddEndpoint(string path, SimpleHealthCheckOptions options)
    {
        return AddEndpoint(new PathString(path), options);
    }

    private ISimpleHealthChecksBuilder AddEndpoint(PathString path, SimpleHealthCheckOptions options)
    {
        services.AddSingleton(new SimpleHealthCheckOptionsMap(path, options));
        return this;
    }
}
