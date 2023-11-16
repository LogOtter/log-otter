using System.Net;

namespace LogOtter.SimpleHealthChecks;

internal class HttpListenerContextShim(HttpListenerContext context) : IHttpListenerContext
{
    public IHttpListenerRequest Request => new HttpListenerRequestShim(context.Request);

    public IHttpListenerResponse Response => new HttpListenerResponseShim(context.Response);
}
