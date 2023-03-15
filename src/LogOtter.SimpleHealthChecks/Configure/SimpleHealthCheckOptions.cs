using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogOtter.SimpleHealthChecks;

public class SimpleHealthCheckOptions
{
    private static readonly IReadOnlyDictionary<HealthStatus, int> DefaultStatusCodesMapping = new Dictionary<HealthStatus, int>
    {
        { HealthStatus.Healthy, 200 }, { HealthStatus.Degraded, 200 }, { HealthStatus.Unhealthy, 503 }
    };

    private IDictionary<HealthStatus, int> _resultStatusCodes = new Dictionary<HealthStatus, int>(DefaultStatusCodesMapping);

    public bool AllowCachingResponses { get; set; }

    public Func<HealthCheckRegistration, bool>? Predicate { get; set; }

    public IDictionary<HealthStatus, int> ResultStatusCodes
    {
        get => _resultStatusCodes;
        set => _resultStatusCodes = ValidateStatusCodesMapping(value);
    }

    public Func<IHttpListenerContext, HealthReport, Task> ResponseWriter { get; set; } = HealthCheckResponseWriters.WriteMinimalPlaintext;

    private static IDictionary<HealthStatus, int> ValidateStatusCodesMapping(IDictionary<HealthStatus, int> mapping)
    {
        var missingHealthStatus = Enum.GetValues<HealthStatus>().Except(mapping.Keys).ToList();
        if (missingHealthStatus.Count == 0)
        {
            return mapping;
        }

        var missing = string.Join(", ", missingHealthStatus.Select(status => $"{nameof(HealthStatus)}.{status}"));
        var message = $"The {nameof(ResultStatusCodes)} dictionary must include an entry for all possible " +
            $"{nameof(HealthStatus)} values. Missing: {missing}";

        throw new InvalidOperationException(message);
    }
}
