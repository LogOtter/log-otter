using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class OverrideResponseTests
{
    [Theory]
    [InlineData(HealthStatus.Healthy, "{\"Status\":\"Healthy\"}")]
    [InlineData(HealthStatus.Degraded, "{\"Status\":\"Degraded\"}")]
    [InlineData(HealthStatus.Unhealthy, "{\"Status\":\"Unhealthy\"}")]
    public async Task ReturnsCorrectResponse(HealthStatus status, string expectedBody)
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.HealthCheckService.ReturnsHealthStatus(status);

        serviceBuilder
            .Endpoints
            .AddEndpoint(
                "/health",
                new SimpleHealthCheckOptions
                {
                    ResponseWriter = async (context, report) =>
                    {
                        var json = JsonSerializer.Serialize(new { Status = report.Status.ToString() });

                        var res = context.Response;
                        res.ContentType = "application/json";
                        res.AddHeader("X-PoweredBy", "LogOtter");

                        await res.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(json));
                    }
                }
            );

        var response = serviceBuilder.EnqueueGetRequest("/health");

        var service = serviceBuilder.Build();

        await service.Run(async () =>
        {
            await response.WaitForResponseClosed();

            response.Headers["X-PoweredBy"].Should().Be("LogOtter");

            var responseBody = Encoding.UTF8.GetString(response.GetOutputStreamBytes());
            responseBody.Should().Be(expectedBody);
        });
    }
}
