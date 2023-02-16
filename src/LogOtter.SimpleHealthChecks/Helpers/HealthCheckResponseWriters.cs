using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogOtter.SimpleHealthChecks;

internal static class HealthCheckResponseWriters
{
    private static readonly byte[] DegradedBytes = Encoding.UTF8.GetBytes(HealthStatus.Degraded.ToString());
    private static readonly byte[] HealthyBytes = Encoding.UTF8.GetBytes(HealthStatus.Healthy.ToString());
    private static readonly byte[] UnhealthyBytes = Encoding.UTF8.GetBytes(HealthStatus.Unhealthy.ToString());

    public static async Task WriteMinimalPlaintext(IHttpListenerContext context, HealthReport result)
    {
        var response = context.Response;

        response.ContentType = "text/plain";

        var bytes = result.Status switch
        {
            HealthStatus.Degraded => DegradedBytes,
            HealthStatus.Healthy => HealthyBytes,
            HealthStatus.Unhealthy => UnhealthyBytes,
            _ => Encoding.UTF8.GetBytes(result.Status.ToString())
        };

        await response.OutputStream.WriteAsync(bytes);
    }
}
