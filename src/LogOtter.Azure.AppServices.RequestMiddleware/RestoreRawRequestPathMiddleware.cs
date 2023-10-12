using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LogOtter.Azure.AppServices.RequestMiddleware;

public class RestoreRawRequestPathMiddleware
{
    private const string UnencodedUrlHeaderName = "X-Waws-Unencoded-Url";

    private static readonly Regex MultipleSlashesRegex = new(@"(\\+)");

    private readonly RequestDelegate _next;
    private readonly ILogger<RestoreRawRequestPathMiddleware> _logger;

    public RestoreRawRequestPathMiddleware(RequestDelegate next, ILogger<RestoreRawRequestPathMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var request = context.Request;

        if (!request.Headers.TryGetValue(UnencodedUrlHeaderName, out var unencodedUrlHeader) || !unencodedUrlHeader.Any())
        {
            await _next(context);
            return;
        }

        var unencodedPath = unencodedUrlHeader.First();
        if (!ValidHeader(request.Path, unencodedPath))
        {
            _logger.LogWarning(
                "Mismatch between request path '{RequestPath}' and X-Waws-Unencoded-Url header '{UnencodedHeaderPath}'",
                request.Path,
                unencodedPath
            );
            await _next(context);
            return;
        }

        request.Path = new PathString(unencodedPath);
        await _next(context);
    }

    private static bool ValidHeader(string pathInUrl, string? unencodedPath)
    {
        if (unencodedPath == null)
        {
            return false;
        }

        var encodedPath = Regex.Replace(WebUtility.UrlDecode(unencodedPath), "/+", "/");

        return string.Equals(pathInUrl, encodedPath, StringComparison.Ordinal);
    }
}
