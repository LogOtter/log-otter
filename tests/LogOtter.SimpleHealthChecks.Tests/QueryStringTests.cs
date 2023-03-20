using FluentAssertions;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class QueryStringTests
{
    [Fact]
    public async Task ReturnsNotFoundWhenQueryStringUsedButNotMapped()
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.Endpoints.AddEndpoint("/health");

        var response = serviceBuilder.EnqueueGetRequest("/health?q=foo");

        var service = serviceBuilder.Build();

        await service.Run(async () =>
        {
            await response.WaitForResponseClosed();
            response.StatusCode.Should().Be(404);
        });
    }

    [Fact]
    public async Task ReturnsOkQueryStringUsed()
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.Endpoints.AddEndpoint("/health?q=foo");

        var response = serviceBuilder.EnqueueGetRequest("/health?q=foo");

        var service = serviceBuilder.Build();

        await service.Run(async () =>
        {
            await response.WaitForResponseClosed();
            response.StatusCode.Should().Be(200);
        });
    }
}
