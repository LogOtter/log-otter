using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogOtter.SimpleHealthChecks;

internal class SimpleHealthCheckService : BackgroundService
{
    private static readonly byte[] OkBytes = Encoding.UTF8.GetBytes("OK");

    private readonly HealthCheckService _healthCheckService;
    private readonly SimpleHealthCheckHostOptions _hostOptions;
    private readonly IHttpListener _listener;
    private readonly ILogger<SimpleHealthCheckService> _logger;
    private readonly IEnumerable<SimpleHealthCheckOptionsMap> _requestMaps;

    public SimpleHealthCheckService(
        HealthCheckService healthCheckService,
        IHttpListenerFactory httpListenerFactory,
        IEnumerable<SimpleHealthCheckOptionsMap> requestMaps,
        IOptions<SimpleHealthCheckHostOptions> options,
        ILogger<SimpleHealthCheckService> logger)
    {
        _healthCheckService = healthCheckService;
        _requestMaps = requestMaps;
        _hostOptions = options.Value;
        _logger = logger;
        _listener = httpListenerFactory.Create();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _listener.Configure(_hostOptions.Scheme, _hostOptions.Hostname, _hostOptions.Port);
            _listener.Start();

            _logger.LogInformation(
                "SimpleHealthCheckService started on {Scheme}://{Host}:{Port}",
                _hostOptions.Scheme,
                _hostOptions.Hostname,
                _hostOptions.Port);

            while (!stoppingToken.IsCancellationRequested)
            {
                var httpContext = await _listener.GetContextAsync();

                try
                {
                    await ProcessHealthCheck(httpContext, stoppingToken);
                }
                finally
                {
                    httpContext.Response.Close();
                }
            }

            _listener.Stop();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SimpleHealthCheckService: {Message}", e.Message);
        }
    }

    private async Task ProcessHealthCheck(IHttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        _logger.LogInformation(
            "SimpleHealthCheckService received a request from {Endpoint} to {RequestPath}",
            request.RemoteEndPoint,
            request.Url!.PathAndQuery);

        var response = context.Response;

        if (!string.Equals(request.HttpMethod, "GET", StringComparison.InvariantCultureIgnoreCase))
        {
            response.StatusCode = 404;
            return;
        }

        var requestPath = new PathString(request.Url!.PathAndQuery);

        if (requestPath.Equals(PathString.Root))
        {
            response.StatusCode = 200;
            response.ContentType = "text/plain";
            await response.OutputStream.WriteAsync(OkBytes, cancellationToken);
            return;
        }

        var options = _requestMaps.Where(map => map.Path.StartsWithSegments(requestPath, out var remaining) && !remaining.HasValue)
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
            var message = $"No status code mapping found for {nameof(HealthStatus)} value: {result.Status}." +
                $"{nameof(SimpleHealthCheckOptions)}.{nameof(SimpleHealthCheckOptions.ResultStatusCodes)} must contain" +
                $"an entry for {result.Status}.";

            throw new InvalidOperationException(message);
        }

        response.StatusCode = statusCode;

        if (!options.AllowCachingResponses)
        {
            response.AddHeader("Cache-Control", "no-store, no-cache");
            response.AddHeader("Pragma", "no-cache");
            response.AddHeader("Expires", "Thu, 01 Jan 1970 00:00:00 GMT");
        }

        await options.ResponseWriter(context, result);
    }
}
