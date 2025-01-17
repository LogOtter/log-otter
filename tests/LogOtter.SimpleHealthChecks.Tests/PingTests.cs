using System.Text;
using Shouldly;
using Xunit;

namespace LogOtter.SimpleHealthChecks.Tests;

public class PingTests
{
    [Fact]
    public async Task PingResponse()
    {
        var serviceBuilder = new TestHealthCheckServiceBuilder();

        serviceBuilder.Endpoints.AddEndpoint("/health");

        var response = serviceBuilder.EnqueueGetRequest("/");

        var service = serviceBuilder.Build();

        await service.Run(async () =>
        {
            await response.WaitForResponseClosed();
            response.ContentType.ShouldBe("text/plain");

            var body = Encoding.UTF8.GetString(response.GetOutputStreamBytes());
            body.ShouldBe("OK");
        });
    }
}
