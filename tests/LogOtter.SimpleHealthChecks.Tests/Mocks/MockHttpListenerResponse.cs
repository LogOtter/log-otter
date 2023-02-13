using System.Net;
using System.Text;

namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockHttpListenerResponse : IHttpListenerResponse
{
    private readonly MemoryStream _outputStream;
    private bool _responseClosed;

    public int StatusCode { get; set; }

    public string? ContentType { get; set; }

    public Encoding? ContentEncoding { get; set; }

    public long ContentLength64 { get; set; }

    public WebHeaderCollection Headers { get; set; }

    public Stream OutputStream => _outputStream;

    public MockHttpListenerResponse()
    {
        _outputStream = new MemoryStream();
        Headers = new WebHeaderCollection();
    }

    public void AddHeader(string name, string value)
    {
        Headers.Add(name, value);
    }

    public void Close()
    {
        _responseClosed = true;
    }

    public async Task WaitForResponseClosed()
    {
        await WaitForResponseClosed(TimeSpan.FromSeconds(1));
    }

    public async Task WaitForResponseClosed(TimeSpan delay)
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(delay);

        await Task.Run(async () =>
        {
            while (!_responseClosed)
            {
                await Task.Delay(100, source.Token);
            }
        }, source.Token);
    }

    public byte[] GetOutputStreamBytes()
    {
        return _outputStream.ToArray();
    }
}
