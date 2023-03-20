using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LogOtter.SimpleHealthChecks.Tests;

internal class TestHealthCheckServiceBuilder
{
    private readonly Queue<MockHttpListenerContext> _contextQueue;
    private readonly MockEndpointsBuilder _endpoints;
    private readonly IHttpListenerFactory _httpListenerFactory;
    private SimpleHealthCheckHostOptions _options;

    public ISimpleHealthChecksBuilder Endpoints => _endpoints;

    public MockHealthCheckService HealthCheckService { get; }

    public TestHealthCheckServiceBuilder()
    {
        _contextQueue = new Queue<MockHttpListenerContext>();
        _httpListenerFactory = new MockHttpListenerFactory(_contextQueue);
        _endpoints = new MockEndpointsBuilder();
        _options = new SimpleHealthCheckHostOptions();

        HealthCheckService = new MockHealthCheckService();
    }

    public void UsingOptions(SimpleHealthCheckHostOptions options)
    {
        _options = options;
    }

    public MockHttpListenerResponse EnqueueRequest(MockHttpListenerRequest request)
    {
        var context = new MockHttpListenerContext(request);
        _contextQueue.Enqueue(context);
        return (MockHttpListenerResponse)context.Response;
    }

    public MockHttpListenerResponse EnqueueGetRequest(string url)
    {
        var request = new MockHttpListenerRequest
        {
            HttpMethod = "GET",
            Url = new Uri($"http://localhost:80{url}"),
            IsLocal = true
        };

        return EnqueueRequest(request);
    }

    public SimpleHealthCheckService Build()
    {
        _options = new SimpleHealthCheckHostOptions();
        return new SimpleHealthCheckService(
            HealthCheckService,
            _httpListenerFactory,
            _endpoints.Maps,
            Options.Create(_options),
            new NullLogger<SimpleHealthCheckService>()
        );
    }
}
