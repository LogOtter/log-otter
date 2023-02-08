using System.Net;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogOtter.SimpleHealthChecks;

internal class SimpleHealthCheckService : BackgroundService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<SimpleHealthCheckService> _logger;
    private readonly SimpleHealthCheckOptions _config;
    private readonly HttpListener _listener = new();

    public SimpleHealthCheckService(
        HealthCheckService healthCheckService,
        IOptions<SimpleHealthCheckOptions> options,
        ILogger<SimpleHealthCheckService> logger
    )
    {
        _healthCheckService = healthCheckService;
        _config = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _listener.Prefixes.Add($"{_config.Scheme}://{_config.Hostname}:{_config.Port}/");
            _listener.Start();

            _logger.LogInformation(
                "SimpleHealthCheckService started on {Host}:{Port}", _config.Hostname, _config.Port);

            while (!cancellationToken.IsCancellationRequested)
            {
                var httpContext = await _listener.GetContextAsync().ConfigureAwait(false);

                ThreadPool.QueueUserWorkItem(
                    async x => await ProcessHealthCheck((HttpListenerContext)x, cancellationToken)
                        .ConfigureAwait(false), httpContext);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ERROR: SimpleHealthCheckService: {Message}", e.Message);
        }
    }

    private async Task ProcessHealthCheck(HttpListenerContext client, CancellationToken cancellationToken)
    {
        var request = client.Request;

        _logger.LogInformation(
            "SimpleHealthCheckService received a request from {Endpoint}", request.RemoteEndPoint);

        using var response = client.Response;

        if (!request.HttpMethod.Equals("GET", StringComparison.InvariantCultureIgnoreCase)
            || !request.Url!.PathAndQuery.Equals(_config.UrlPath, StringComparison.InvariantCultureIgnoreCase))
        {
            response.StatusCode = 404;
            return;
        }

        var healthReport = await _healthCheckService.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

        response.ContentType = "text/plain";
        response.ContentEncoding = Encoding.UTF8;

        var status = healthReport.Status == HealthStatus.Healthy
            ? HttpStatusCode.OK
            : HttpStatusCode.ServiceUnavailable;

        response.StatusCode = (int)status;
        var data = Encoding.UTF8.GetBytes(healthReport.Status.ToString());

        response.ContentLength64 = data.Length;

        await response.OutputStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
    }
}