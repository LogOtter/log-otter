namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockHttpListener : IHttpListener
{
    private readonly Queue<MockHttpListenerContext> _contextQueue;

    public string? Scheme { get; private set; }

    public string? Hostname { get; private set; }

    public int? Port { get; private set; }

    public bool IsRunning { get; private set; }

    public MockHttpListener(Queue<MockHttpListenerContext> contextQueue)
    {
        _contextQueue = contextQueue;
    }

    public void Configure(string scheme, string hostname, int port)
    {
        Scheme = scheme;
        Hostname = hostname;
        Port = port;
    }

    public void Start()
    {
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public Task<IHttpListenerContext> GetContextAsync()
    {
        var context = _contextQueue.Dequeue();
        return Task.FromResult<IHttpListenerContext>(context);
    }
}
