using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockHttpListenerRequest : IHttpListenerRequest
{
    private readonly MemoryStream _inputStream;

    public MockHttpListenerRequest()
    {
        _inputStream = new MemoryStream();
    }

    public string HttpMethod { get; init; } = "";
    public Uri? Url { get; init; }
    public IPEndPoint RemoteEndPoint { get; init; } = IPEndPoint.Parse("127.0.0.1:45678");
    public bool IsLocal { get; init; }
    public string? ContentType { get; init; }
    public long ContentLength64 { get; init; }
    public Encoding ContentEncoding { get; init; } = Encoding.UTF8;
    public NameValueCollection Headers { get; } = new();
    public Stream InputStream => _inputStream;
}
