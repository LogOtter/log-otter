using System.Reflection;
using LogOtter.CosmosDb.EventStore.EventStreamApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.EventStreamUI;

internal class EventStreamsUIMiddleware
{
    private readonly IWebHostEnvironment _hostEnvironment;
    private const string EmbeddedFileNamespace = "LogOtter.CosmosDb.EventStore.EventStreamUI.wwwroot";

    private readonly StaticFileMiddleware _staticFileMiddleware;
    private readonly PathString _rootPath;
    private readonly PathString _apiRoutePrefix;

    public EventStreamsUIMiddleware(
        RequestDelegate next,
        IWebHostEnvironment hostEnvironment,
        ILoggerFactory loggerFactory,
        EventStreamsUIOptions options,
        EventStreamsApiOptionsContainer apiOptionsContainer
    )
    {
        _hostEnvironment = hostEnvironment;
        _rootPath = new PathString(options.RoutePrefix).EnsurePathDoesNotEndWithSlash();
        _staticFileMiddleware = CreateStaticFileMiddleware(next, hostEnvironment, loggerFactory, options);
        _apiRoutePrefix = apiOptionsContainer.Options.RoutePrefix;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var currentPath = httpContext.Request.Path.EnsurePathDoesNotEndWithSlash();
        if (currentPath == _rootPath)
        {
            if (!httpContext.Request.Path.EndsWithSlash())
            {
                httpContext.Response.Redirect(_rootPath + "/");
                return;
            }

            httpContext.Request.Path = _rootPath + "/index.html";
        } else if (currentPath.StartsWithSegments($"{_rootPath}/config", out var remaining) && remaining == PathString.Empty)
        {
            WriteConfigJson(httpContext.Response);
            return;
        }

        await _staticFileMiddleware.Invoke(httpContext);
    }

    private void WriteConfigJson(HttpResponse response)
    {
        var config = new
        {
            ApiBaseUrl = _apiRoutePrefix.Value
        };

        if (_hostEnvironment.IsDevelopment())
        {
            response.Headers.AccessControlAllowOrigin = "*";
        }

        response.WriteAsJsonAsync(config);
    }

    private static StaticFileMiddleware CreateStaticFileMiddleware(
        RequestDelegate next,
        IWebHostEnvironment hostingEnv,
        ILoggerFactory loggerFactory,
        EventStreamsUIOptions options
    )
    {
        var staticFileOptions = new StaticFileOptions
        {
            RequestPath = string.IsNullOrEmpty(options.RoutePrefix)
                ? string.Empty
                : $"/{options.RoutePrefix.Trim('/')}",
            FileProvider = new EmbeddedFileProvider(
                typeof(EventStreamsUIMiddleware).GetTypeInfo().Assembly,
                EmbeddedFileNamespace
            )
        };

        return new StaticFileMiddleware(next, hostingEnv, Options.Create(staticFileOptions), loggerFactory);
    }
}
