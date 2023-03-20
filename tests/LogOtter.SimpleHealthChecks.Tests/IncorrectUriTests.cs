using FluentAssertions;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class IncorrectUriTests
{
    [Fact]
    public async Task ReturnsNotFoundWhenNotMapped()
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        var response = serviceBuilder.EnqueueGetRequest("/health");

        var service = serviceBuilder.Build();

        await service.Run(async () =>
        {
            await response.WaitForResponseClosed();
            response.StatusCode.Should().Be(404);
        });
    }

    [Fact]
    public async Task ReturnsNotFoundWhenMappedIncorrectly()
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.Endpoints.AddEndpoint("/health2");

        var response = serviceBuilder.EnqueueGetRequest("/health");

        var service = serviceBuilder.Build();

        await service.Run(async () =>
        {
            await response.WaitForResponseClosed();
            response.StatusCode.Should().Be(404);
        });
    }
}
