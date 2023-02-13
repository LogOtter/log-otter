using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CustomerWorker.HealthChecks;

public class AlwaysHealthyCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = HealthCheckResult.Healthy();

        return Task.FromResult(result);
    }
}
