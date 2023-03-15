using System.Net;
using FluentAssertions;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class CacheTests
{
    [Fact]
    public async Task ReturnsNoCacheHeaders()
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.Endpoints.AddEndpoint("/health");

        var response = serviceBuilder.EnqueueGetRequest("/health");

        var service = serviceBuilder.Build();

        await service.Run(
            async () =>
            {
                await response.WaitForResponseClosed();

                response.Headers[HttpResponseHeader.CacheControl].Should().Be("no-store, no-cache");
                response.Headers[HttpResponseHeader.Pragma].Should().Be("no-cache");
                response.Headers[HttpResponseHeader.Expires].Should().Be("Thu, 01 Jan 1970 00:00:00 GMT");
            });
    }

    [Fact]
    public async Task ReturnsCacheHeaders()
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.Endpoints.AddEndpoint("/health", new SimpleHealthCheckOptions { AllowCachingResponses = true });

        var response = serviceBuilder.EnqueueGetRequest("/health");

        var service = serviceBuilder.Build();

        await service.Run(
            async () =>
            {
                await response.WaitForResponseClosed();

                response.Headers[HttpResponseHeader.CacheControl].Should().BeNull();
                response.Headers[HttpResponseHeader.Pragma].Should().BeNull();
                response.Headers[HttpResponseHeader.Expires].Should().BeNull();
            });
    }
}
