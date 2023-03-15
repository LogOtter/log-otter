using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace LogOtter.SimpleHealthChecks;

internal class HttpListenerRequestShim : IHttpListenerRequest
{
    private readonly HttpListenerRequest _request;

    public HttpListenerRequestShim(HttpListenerRequest request)
    {
        _request = request;
    }

    public string HttpMethod => _request.HttpMethod;
    public Uri? Url => _request.Url;
    public IPEndPoint RemoteEndPoint => _request.RemoteEndPoint;

    public bool IsLocal => _request.IsLocal;

    public string? ContentType => _request.ContentType;

    public long ContentLength64 => _request.ContentLength64;

    public Encoding ContentEncoding => _request.ContentEncoding;

    public NameValueCollection Headers => _request.Headers;

    public Stream InputStream => _request.InputStream;
}
