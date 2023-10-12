using Microsoft.AspNetCore.Http;

namespace LogOtter.Azure.AppServices.RequestMiddleware;

public class RestoreRawRequestPathMiddleware
{
    private const string UnencodedUrlHeaderName = "X-Waws-Unencoded-Url";

    private readonly RequestDelegate _next;

    public RestoreRawRequestPathMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(UnencodedUrlHeaderName, out var unencodedUrlValue) && unencodedUrlValue.Any())
        {
            context.Request.Path = new PathString(unencodedUrlValue.First());
        }

        await _next(context);
    }
}
