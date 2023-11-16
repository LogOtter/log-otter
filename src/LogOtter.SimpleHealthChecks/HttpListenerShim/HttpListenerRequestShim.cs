using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace LogOtter.SimpleHealthChecks;

internal class HttpListenerRequestShim(HttpListenerRequest request) : IHttpListenerRequest
{
    public string HttpMethod => request.HttpMethod;
    public Uri? Url => request.Url;
    public IPEndPoint RemoteEndPoint => request.RemoteEndPoint;

    public bool IsLocal => request.IsLocal;

    public string? ContentType => request.ContentType;

    public long ContentLength64 => request.ContentLength64;

    public Encoding ContentEncoding => request.ContentEncoding;

    public NameValueCollection Headers => request.Headers;

    public Stream InputStream => request.InputStream;
}
