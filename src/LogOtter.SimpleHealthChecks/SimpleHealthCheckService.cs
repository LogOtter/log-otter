using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogOtter.SimpleHealthChecks;

internal class SimpleHealthCheckService : BackgroundService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IEnumerable<SimpleHealthCheckOptionsMap> _requestMaps;
    private readonly ILogger<SimpleHealthCheckService> _logger;
    private readonly SimpleHealthCheckHostOptions _hostOptions;
    private readonly HttpListener _listener = new();

    public SimpleHealthCheckService(
        HealthCheckService healthCheckService,
        IEnumerable<SimpleHealthCheckOptionsMap> requestMaps,
        IOptions<SimpleHealthCheckHostOptions> options,
        ILogger<SimpleHealthCheckService> logger
    )
    {
        _healthCheckService = healthCheckService;
        _requestMaps = requestMaps;
        _hostOptions = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _listener.Prefixes.Add($"{_hostOptions.Scheme}://{_hostOptions.Hostname}:{_hostOptions.Port}/");
            _listener.Start();

            _logger.LogInformation("SimpleHealthCheckService started on {Scheme}://{Host}:{Port}",
                _hostOptions.Scheme,
                _hostOptions.Hostname,
                _hostOptions.Port
            );

            while (!cancellationToken.IsCancellationRequested)
            {
                var httpContext = await _listener.GetContextAsync();
                await ProcessHealthCheck(httpContext, cancellationToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ERROR: SimpleHealthCheckService: {Message}", e.Message);
        }
    }

    private async Task ProcessHealthCheck(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        _logger.LogInformation(
            "SimpleHealthCheckService received a request from {Endpoint}", request.RemoteEndPoint);

        using var response = context.Response;

        if (!string.Equals(request.HttpMethod, "GET", StringComparison.InvariantCultureIgnoreCase))
        {
            response.StatusCode = 404;
            return;
        }

        var requestPath = new PathString(request.Url!.PathAndQuery);

        var options = _requestMaps
            .Where(map => map.Path.StartsWithSegments(requestPath, out var remaining) && !remaining.HasValue)
            .Select(map => map.Options)
            .FirstOrDefault();

        if (options == null)
        {
            response.StatusCode = 404;
            return;
        }

        var result = await _healthCheckService.CheckHealthAsync(options.Predicate, cancellationToken);

        if (!options.ResultStatusCodes.TryGetValue(result.Status, out var statusCode))
        {
            var message =
                $"No status code mapping found for {nameof(HealthStatus)} value: {result.Status}." +
                $"{nameof(SimpleHealthCheckOptions)}.{nameof(SimpleHealthCheckOptions.ResultStatusCodes)} must contain" +
                $"an entry for {result.Status}.";

            throw new InvalidOperationException(message);
        }

        response.StatusCode = statusCode;

        if (!options.AllowCachingResponses)
        {
            var headers = response.Headers;
            headers[HttpResponseHeader.CacheControl] = "no-store, no-cache";
            headers[HttpResponseHeader.Pragma] = "no-cache";
            headers[HttpResponseHeader.Expires] = "Thu, 01 Jan 1970 00:00:00 GMT";
        }

        await options.ResponseWriter(context, result);
    }
}
