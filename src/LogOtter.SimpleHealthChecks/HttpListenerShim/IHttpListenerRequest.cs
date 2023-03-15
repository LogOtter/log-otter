using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace LogOtter.SimpleHealthChecks;

public interface IHttpListenerRequest
{
    string HttpMethod { get; }
    Uri? Url { get; }
    IPEndPoint RemoteEndPoint { get; }
    bool IsLocal { get; }
    string? ContentType { get; }
    long ContentLength64 { get; }
    Encoding ContentEncoding { get; }
    NameValueCollection Headers { get; }
    Stream InputStream { get; }
}
