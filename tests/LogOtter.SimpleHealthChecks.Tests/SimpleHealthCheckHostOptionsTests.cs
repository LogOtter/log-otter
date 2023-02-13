using FluentAssertions;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class SimpleHealthCheckHostOptionsTests
{
    [Fact]
    public void DefaultHost()
    {
        var options = new SimpleHealthCheckHostOptions();

        options.Hostname.Should().Be("+");
    }

    [Fact]
    public void DefaultPort()
    {
        var options = new SimpleHealthCheckHostOptions();

        options.Port.Should().Be(80);
    }

    [Fact]
    public void DefaultScheme()
    {
        var options = new SimpleHealthCheckHostOptions();

        options.Scheme.Should().Be("http");
    }
}
