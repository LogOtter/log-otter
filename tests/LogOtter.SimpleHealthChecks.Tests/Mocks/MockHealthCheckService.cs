using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockHealthCheckService : HealthCheckService
{
    private readonly IDictionary<string, HealthReportEntry> _entries = new Dictionary<string, HealthReportEntry>();
    private HealthStatus _status = HealthStatus.Healthy;
    private TimeSpan _duration = TimeSpan.FromMilliseconds(10);

    public MockHealthCheckService ReturnsHealthStatus(HealthStatus status)
    {
        _status = status;
        return this;
    }

    public MockHealthCheckService ReturnsHealthReportEntry(string name, HealthReportEntry entry)
    {
        _entries.Add(name, entry);
        return this;
    }

    public MockHealthCheckService ReturnsDuration(TimeSpan duration)
    {
        _duration = duration;
        return this;
    }

    public override Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool>? predicate, CancellationToken cancellationToken = default)
    {
        var report = new HealthReport(
            _entries.AsReadOnly(),
            _status,
            _duration
        );

        return Task.FromResult(report);
    }
}
