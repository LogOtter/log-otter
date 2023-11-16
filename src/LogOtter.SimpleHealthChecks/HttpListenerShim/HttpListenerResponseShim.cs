using System.Net;
using System.Text;

namespace LogOtter.SimpleHealthChecks;

internal class HttpListenerResponseShim(HttpListenerResponse response) : IHttpListenerResponse
{
    public int StatusCode
    {
        get => response.StatusCode;
        set => response.StatusCode = value;
    }

    public string? ContentType
    {
        get => response.ContentType;
        set => response.ContentType = value;
    }

    public Encoding? ContentEncoding
    {
        get => response.ContentEncoding;
        set => response.ContentEncoding = value;
    }

    public long ContentLength64
    {
        get => response.ContentLength64;
        set => response.ContentLength64 = value;
    }

    public WebHeaderCollection Headers
    {
        get => response.Headers;
        set => response.Headers = value;
    }

    public Stream OutputStream => response.OutputStream;

    public void AddHeader(string name, string value)
    {
        response.AddHeader(name, value);
    }

    public void Close()
    {
        response.Close();
    }
}
