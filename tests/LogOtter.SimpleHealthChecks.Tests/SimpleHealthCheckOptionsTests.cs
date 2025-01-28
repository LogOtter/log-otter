using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class SimpleHealthCheckOptionsTests
{
    [Fact]
    public void DefaultOptions()
    {
        var options = new SimpleHealthCheckOptions();

        options.ResultStatusCodes.Count.ShouldBe(3);
        options.ResultStatusCodes.ShouldContainKey(HealthStatus.Healthy);
        options.ResultStatusCodes.ShouldContainKey(HealthStatus.Degraded);
        options.ResultStatusCodes.ShouldContainKey(HealthStatus.Unhealthy);
        options.ResultStatusCodes[HealthStatus.Healthy].ShouldBe(200);
        options.ResultStatusCodes[HealthStatus.Degraded].ShouldBe(200);
        options.ResultStatusCodes[HealthStatus.Unhealthy].ShouldBe(503);

        options.AllowCachingResponses.ShouldBeFalse();
    }

    [Fact]
    public void ThrowsExceptionWhenIncompleteDictionarySpecified()
    {
        var invalidOptions = new Dictionary<HealthStatus, int> { [HealthStatus.Degraded] = 230 };

        var actions = () => new SimpleHealthCheckOptions { ResultStatusCodes = invalidOptions };

        actions.ShouldThrow<InvalidOperationException>();
    }
}
