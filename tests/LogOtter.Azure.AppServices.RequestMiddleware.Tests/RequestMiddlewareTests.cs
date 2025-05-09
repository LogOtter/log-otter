﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace LogOtter.Azure.AppServices.RequestMiddleware.Tests;

public class RequestMiddlewareTests
{
    private static readonly RequestDelegate NoActionRequestDelegate = _ => Task.CompletedTask;

    [Theory]
    [InlineData("/", "/")]
    [InlineData("/foo/bar", "/foo/bar")]
    [InlineData("/foo/bar/customers/1234", "/foo/bar/%2Fcustomers%2F1234")]
    [InlineData("/foo/bar/,customers,1234", "/foo/bar/%2ccustomers%2c1234")]
    [InlineData("/foo/bar/", "/foo/bar/%2f%2f%2f")]
    [InlineData("/foo/bar/customers/1234/body", "/foo/bar/%2Fcustomers%2F1234/body")]
    public async Task RequestMiddleware_RewritesPath(string pathInUrl, string pathInHeader)
    {
        var context = CreateContext(pathInUrl, pathInHeader: pathInHeader);

        var middleware = new RestoreRawRequestPathMiddleware(NoActionRequestDelegate, CreateLogger());
        await middleware.Invoke(context);

        context.Request.Path.ToString().ShouldBe(pathInHeader);
    }

    [Theory]
    [InlineData("/foo/bar/customers/1234/body", "?key=value", "/foo/bar/%2Fcustomers%2F1234/body?key=value", "/foo/bar/%2Fcustomers%2F1234/body")]
    [InlineData(
        "/foo/bar/customers/1234/body",
        "?key=value&key2=value2",
        "/foo/bar/%2Fcustomers%2F1234/body?key=value&key2=value2",
        "/foo/bar/%2Fcustomers%2F1234/body"
    )]
    public async Task RequestMiddleware_RewritesPathWithQueryString(string pathInUrl, string queryString, string pathInHeader, string expectedPath)
    {
        var context = CreateContext(pathInUrl, queryString, pathInHeader);

        var middleware = new RestoreRawRequestPathMiddleware(NoActionRequestDelegate, CreateLogger());
        await middleware.Invoke(context);

        context.Request.Path.ToString().ShouldBe(expectedPath);
        context.Request.QueryString.ToString().ShouldBe(queryString);
    }

    [Fact]
    public async Task RequestMiddleware_NoHeader()
    {
        var context = CreateContext("/foo/bar/customers/1234");

        var middleware = new RestoreRawRequestPathMiddleware(NoActionRequestDelegate, CreateLogger());
        await middleware.Invoke(context);

        context.Request.Path.ToString().ShouldBe("/foo/bar/customers/1234");
    }

    [Fact]
    public async Task RequestMiddleware_MismatchPath()
    {
        var context = CreateContext("/foo/bar/customers/1234", pathInHeader: "/mismatched/path/%2Fcustomers%2F1234");

        var middleware = new RestoreRawRequestPathMiddleware(NoActionRequestDelegate, CreateLogger());
        await middleware.Invoke(context);

        context.Request.Path.ToString().ShouldBe("/foo/bar/customers/1234");
    }

    [Fact]
    public async Task RequestMiddleware_MismatchQuery()
    {
        var context = CreateContext(
            "/foo/bar/customers/1234",
            queryString: "?key=value",
            pathInHeader: "/foo/bar/%2Fcustomers%2F1234?mismatch=different"
        );

        var middleware = new RestoreRawRequestPathMiddleware(NoActionRequestDelegate, CreateLogger());
        await middleware.Invoke(context);

        context.Request.Path.ToString().ShouldBe("/foo/bar/customers/1234");
    }

    private static HttpContext CreateContext(string pathInUrl, string? queryString = null, string? pathInHeader = null)
    {
        var httpContext = new DefaultHttpContext { Request = { Path = pathInUrl, QueryString = new QueryString(queryString) } };

        if (pathInHeader != null)
        {
            httpContext.Request.Headers["X-Waws-Unencoded-Url"] = pathInHeader;
        }

        return httpContext;
    }

    private static ILogger<RestoreRawRequestPathMiddleware> CreateLogger()
    {
        return LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<RestoreRawRequestPathMiddleware>();
    }
}
