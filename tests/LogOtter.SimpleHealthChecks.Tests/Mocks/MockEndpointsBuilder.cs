namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockEndpointsBuilder : ISimpleHealthChecksBuilder
{
    public IList<SimpleHealthCheckOptionsMap> Maps { get; } = new List<SimpleHealthCheckOptionsMap>();

    public ISimpleHealthChecksBuilder AddEndpoint(string path)
    {
        return AddEndpoint(path, new SimpleHealthCheckOptions());
    }

    public ISimpleHealthChecksBuilder AddEndpoint(string path, SimpleHealthCheckOptions options)
    {
        Maps.Add(new SimpleHealthCheckOptionsMap(new PathString(path), options));
        return this;
    }
}
