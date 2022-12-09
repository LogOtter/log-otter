using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore.EventStreamUI;

internal class EventStreamsUIMiddleware
{
    private const string EmbeddedFileNamespace = "LogOtter.CosmosDb.EventStore.EventStreamUI.wwwroot";

    private readonly RequestDelegate _next;
    private readonly EventStreamsUIOptions _options;
    private readonly StaticFileMiddleware _staticFileMiddleware;

    public EventStreamsUIMiddleware(
        RequestDelegate next,
        IWebHostEnvironment hostingEnv,
        ILoggerFactory loggerFactory,
        EventStreamsUIOptions options
    )
    {
        _next = next;
        _options = options;

        _staticFileMiddleware = CreateStaticFileMiddleware(next, hostingEnv, loggerFactory, options);
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        await _staticFileMiddleware.Invoke(httpContext);
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
            RequestPath = string.IsNullOrEmpty(options.RoutePrefix) ? string.Empty : $"/{options.RoutePrefix.TrimStart('/')}",
            FileProvider = new EmbeddedFileProvider(typeof(EventStreamsUIMiddleware).GetTypeInfo().Assembly,
                EmbeddedFileNamespace),
        };

        return new StaticFileMiddleware(next, hostingEnv, Options.Create(staticFileOptions), loggerFactory);
    }
}