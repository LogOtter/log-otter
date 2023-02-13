using System.Net;
using System.Text;

namespace LogOtter.SimpleHealthChecks;

internal class HttpListenerResponseShim : IHttpListenerResponse
{
    private readonly HttpListenerResponse _response;

    public HttpListenerResponseShim(HttpListenerResponse response)
    {
        _response = response;
    }

    public int StatusCode
    {
        get => _response.StatusCode;
        set => _response.StatusCode = value;
    }

    public string? ContentType
    {
        get => _response.ContentType;
        set => _response.ContentType = value;
    }

    public Encoding? ContentEncoding
    {
        get => _response.ContentEncoding;
        set => _response.ContentEncoding = value;
    }

    public long ContentLength64
    {
        get => _response.ContentLength64;
        set => _response.ContentLength64 = value;
    }

    public WebHeaderCollection Headers
    {
        get => _response.Headers;
        set => _response.Headers = value;
    }

    public Stream OutputStream => _response.OutputStream;

    public void AddHeader(string name, string value)
    {
        _response.AddHeader(name, value);
    }

    public void Close()
    {
        _response.Close();
    }
}
