using System.Net;
using System.Text;

namespace LogOtter.SimpleHealthChecks;

public interface IHttpListenerResponse
{
    int StatusCode { get; set; }
    string? ContentType { get; set; }
    Encoding? ContentEncoding { get; set; }
    long ContentLength64 { get; set; }
    WebHeaderCollection Headers { get; set; }
    Stream OutputStream { get; }

    void AddHeader(string name, string value);
    void Close();
}
