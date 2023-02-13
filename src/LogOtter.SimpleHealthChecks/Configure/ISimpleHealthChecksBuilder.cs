namespace LogOtter.SimpleHealthChecks;

public interface ISimpleHealthChecksBuilder
{
    ISimpleHealthChecksBuilder AddEndpoint(string path);
    ISimpleHealthChecksBuilder AddEndpoint(string path, SimpleHealthCheckOptions options);
}
