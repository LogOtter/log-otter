using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class SimpleHealthCheckOptionsTests
{
    [Fact]
    public void DefaultOptions()
    {
        var options = new SimpleHealthCheckOptions();

        options.ResultStatusCodes.Should().HaveCount(3);
        options.ResultStatusCodes.Should().ContainKey(HealthStatus.Healthy);
        options.ResultStatusCodes.Should().ContainKey(HealthStatus.Degraded);
        options.ResultStatusCodes.Should().ContainKey(HealthStatus.Unhealthy);
        options.ResultStatusCodes[HealthStatus.Healthy].Should().Be(200);
        options.ResultStatusCodes[HealthStatus.Degraded].Should().Be(200);
        options.ResultStatusCodes[HealthStatus.Unhealthy].Should().Be(503);

        options.AllowCachingResponses.Should().BeFalse();
    }

    [Fact]
    public void ThrowsExceptionWhenIncompleteDictionarySpecified()
    {
        var invalidOptions = new Dictionary<HealthStatus, int> { [HealthStatus.Degraded] = 230 };

        var actions = () => new SimpleHealthCheckOptions { ResultStatusCodes = invalidOptions };

        actions.Should().Throw<InvalidOperationException>();
    }
}
