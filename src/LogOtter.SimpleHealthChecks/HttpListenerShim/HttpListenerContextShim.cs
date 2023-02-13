using System.Net;

namespace LogOtter.SimpleHealthChecks;

internal class HttpListenerContextShim : IHttpListenerContext
{
    private readonly HttpListenerContext _context;

    public HttpListenerContextShim(HttpListenerContext context)
    {
        _context = context;
    }

    public IHttpListenerRequest Request => new HttpListenerRequestShim(_context.Request);
    
    public IHttpListenerResponse Response => new HttpListenerResponseShim(_context.Response);
}