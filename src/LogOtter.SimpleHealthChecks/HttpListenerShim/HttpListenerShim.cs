using System.Net;

namespace LogOtter.SimpleHealthChecks;

internal class HttpListenerShim : IHttpListener
{
    private readonly HttpListener _httpListener = new();

    public void Configure(string scheme, string hostname, int port)
    {
        _httpListener.Prefixes.Add($"{scheme}://{hostname}:{port}/");
    }

    public void Start()
    {
        _httpListener.Start();
    }

    public void Stop()
    {
        _httpListener.Stop();
    }

    public async Task<IHttpListenerContext> GetContextAsync()
    {
        var context = await _httpListener.GetContextAsync();
        return new HttpListenerContextShim(context);
    }
}
