using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.SimpleHealthChecks;

internal class SimpleHealthChecksBuilder : ISimpleHealthChecksBuilder
{
    private readonly IServiceCollection _services;

    public SimpleHealthChecksBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public ISimpleHealthChecksBuilder AddEndpoint(string path)
    {
        AddEndpoint(new PathString(path), new SimpleHealthCheckOptions());
        return this;
    }

    public ISimpleHealthChecksBuilder AddEndpoint(string path, SimpleHealthCheckOptions options)
    {
        AddEndpoint(new PathString(path), options);
        return this;
    }

    private ISimpleHealthChecksBuilder AddEndpoint(PathString path, SimpleHealthCheckOptions options)
    {
        _services.AddSingleton(new SimpleHealthCheckOptionsMap(path, options));
        return this;
    }
}
