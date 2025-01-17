using Shouldly;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class SimpleHealthCheckHostOptionsTests
{
    [Fact]
    public void DefaultHost()
    {
        var options = new SimpleHealthCheckHostOptions();

        options.Hostname.ShouldBe("+");
    }

    [Fact]
    public void DefaultPort()
    {
        var options = new SimpleHealthCheckHostOptions();

        options.Port.ShouldBe(80);
    }

    [Fact]
    public void DefaultScheme()
    {
        var options = new SimpleHealthCheckHostOptions();

        options.Scheme.ShouldBe("http");
    }
}
