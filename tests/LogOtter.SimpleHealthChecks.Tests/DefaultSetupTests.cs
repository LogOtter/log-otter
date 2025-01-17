using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class DefaultSetupTests
{
    [Fact]
    public async Task DefaultReturnsTextPlain()
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.Endpoints.AddEndpoint("/health");

        var response = serviceBuilder.EnqueueGetRequest("/health");

        var service = serviceBuilder.Build();

        await service.Run(async () =>
        {
            await response.WaitForResponseClosed();
            response.ContentType.ShouldBe("text/plain");
        });
    }

    [Theory]
    [InlineData(HealthStatus.Healthy, 200)]
    [InlineData(HealthStatus.Degraded, 200)]
    [InlineData(HealthStatus.Unhealthy, 503)]
    public async Task DefaultReturnsCorrectStatusCode(HealthStatus status, int expectedStatusCode)
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.HealthCheckService.ReturnsHealthStatus(status);

        serviceBuilder.Endpoints.AddEndpoint("/health");

        var response = serviceBuilder.EnqueueGetRequest("/health");

        var service = serviceBuilder.Build();

        await service.Run(async () =>
        {
            await response.WaitForResponseClosed();
            response.StatusCode.ShouldBe(expectedStatusCode);
        });
    }

    [Theory]
    [InlineData(HealthStatus.Healthy, "Healthy")]
    [InlineData(HealthStatus.Degraded, "Degraded")]
    [InlineData(HealthStatus.Unhealthy, "Unhealthy")]
    public async Task DefaultReturnsCorrectBody(HealthStatus status, string expectedResponseBody)
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.HealthCheckService.ReturnsHealthStatus(status);

        serviceBuilder.Endpoints.AddEndpoint("/health");

        var response = serviceBuilder.EnqueueGetRequest("/health");

        var service = serviceBuilder.Build();

        await service.Run(async () =>
        {
            await response.WaitForResponseClosed();

            var responseBody = Encoding.UTF8.GetString(response.GetOutputStreamBytes());
            responseBody.ShouldBe(expectedResponseBody);
        });
    }
}
