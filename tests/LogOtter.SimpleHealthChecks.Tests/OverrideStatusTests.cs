using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class OverrideStatusTests
{
    [Theory]
    [InlineData(HealthStatus.Healthy, 230)]
    [InlineData(HealthStatus.Degraded, 231)]
    [InlineData(HealthStatus.Unhealthy, 520)]
    public async Task ReturnsCorrectStatusCode(HealthStatus status, int expectedStatusCode)
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.HealthCheckService.ReturnsHealthStatus(status);

        serviceBuilder
            .Endpoints
            .AddEndpoint(
                "/health",
                new SimpleHealthCheckOptions
                {
                    ResultStatusCodes =
                    {
                        [HealthStatus.Healthy] = 230,
                        [HealthStatus.Degraded] = 231,
                        [HealthStatus.Unhealthy] = 520
                    }
                }
            );

        var response = serviceBuilder.EnqueueGetRequest("/health");

        var service = serviceBuilder.Build();

        await service.Run(async () =>
        {
            await response.WaitForResponseClosed();
            response.StatusCode.Should().Be(expectedStatusCode);
        });
    }
}
