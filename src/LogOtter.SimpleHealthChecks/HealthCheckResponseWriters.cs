using System.Net;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LogOtter.SimpleHealthChecks;

internal static class HealthCheckResponseWriters
{
    private static readonly byte[] DegradedBytes = Encoding.UTF8.GetBytes(HealthStatus.Degraded.ToString());
    private static readonly byte[] HealthyBytes = Encoding.UTF8.GetBytes(HealthStatus.Healthy.ToString());
    private static readonly byte[] UnhealthyBytes = Encoding.UTF8.GetBytes(HealthStatus.Unhealthy.ToString());

    public static Task WriteMinimalPlaintext(HttpListenerContext httpContext, HealthReport result)
    {
        httpContext.Response.ContentType = "text/plain";
        return result.Status switch
        {
            HealthStatus.Degraded => httpContext.Response.OutputStream.WriteAsync(DegradedBytes.AsMemory()).AsTask(),
            HealthStatus.Healthy => httpContext.Response.OutputStream.WriteAsync(HealthyBytes.AsMemory()).AsTask(),
            HealthStatus.Unhealthy => httpContext.Response.OutputStream.WriteAsync(UnhealthyBytes.AsMemory()).AsTask(),
            _ => httpContext.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(result.Status.ToString())).AsTask()
        };
    }
}
