using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogOtter.Azure.AppServices.RequestMiddleware.Tests;

public class RequestMiddlewareTests
{
    private static readonly RequestDelegate NoActionRequestDelegate = _ => Task.CompletedTask;
    private readonly ILogger<RestoreRawRequestPathMiddleware> _middlewareLogger = new NullLogger<RestoreRawRequestPathMiddleware>();

    [Theory]
    [InlineData("/", "/")]
    [InlineData("/foo/bar", "/foo/bar")]
    [InlineData("/foo/bar/customers/1234", "/foo/bar/%2Fcustomers%2F1234")]
    [InlineData("/foo/bar/,customers,1234", "/foo/bar/%2ccustomers%2c1234")]
    [InlineData("/foo/bar/", "/foo/bar/%2f%2f%2f")]
    public async Task RequestMiddleware_RewritesPath(string pathInUrl, string pathInHeader)
    {
        var context = CreateContext(pathInUrl, pathInHeader);

        var middleware = new RestoreRawRequestPathMiddleware(NoActionRequestDelegate, _middlewareLogger);
        await middleware.Invoke(context);

        context.Request.Path.ToString().Should().Be(pathInHeader);
    }

    [Fact]
    public async Task RequestMiddleware_NoHeader()
    {
        var context = CreateContext("/foo/bar/customers/1234");

        var middleware = new RestoreRawRequestPathMiddleware(NoActionRequestDelegate, _middlewareLogger);
        await middleware.Invoke(context);

        context.Request.Path.ToString().Should().Be("/foo/bar/customers/1234");
    }

    [Fact]
    public async Task RequestMiddleware_MismatchPath()
    {
        var context = CreateContext("/foo/bar/customers/1234", "/mismatched/path/%2Fcustomers%2F1234");

        var middleware = new RestoreRawRequestPathMiddleware(NoActionRequestDelegate, _middlewareLogger);
        await middleware.Invoke(context);

        context.Request.Path.ToString().Should().Be("/foo/bar/customers/1234");
    }

    private static HttpContext CreateContext(string pathInUrl, string? pathInHeader = null)
    {
        var httpContext = new DefaultHttpContext { Request = { Path = pathInUrl } };

        if (pathInHeader != null)
        {
            httpContext.Request.Headers["X-Waws-Unencoded-Url"] = pathInHeader;
        }

        return httpContext;
    }
}
