namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockHttpListener(Queue<MockHttpListenerContext> contextQueue) : IHttpListener
{
    public string? Scheme { get; private set; }

    public string? Hostname { get; private set; }

    public int? Port { get; private set; }

    public bool IsRunning { get; private set; }

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
        var context = contextQueue.Dequeue();
        return Task.FromResult<IHttpListenerContext>(context);
    }
}
