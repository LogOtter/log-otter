using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LogOtter.Azure.AppServices.RequestMiddleware;

public class RestoreRawRequestPathMiddleware(RequestDelegate next, ILogger<RestoreRawRequestPathMiddleware> logger)
{
    private const string UnencodedUrlHeaderName = "X-Waws-Unencoded-Url";

    public async Task Invoke(HttpContext context)
    {
        var request = context.Request;

        if (!TryGetUnencodedUrlFromHeader(request, out var unencodedUrl))
        {
            await next(context);
            return;
        }

        if (!ValidHeader(request, unencodedUrl))
        {
            logger.LogWarning(
                "Mismatch between request path '{RequestPath}' and X-Waws-Unencoded-Url header '{UnencodedHeaderPath}'",
                request.Path + request.QueryString,
                unencodedUrl
            );
            await next(context);
            return;
        }

        request.Path = new PathString(unencodedUrl.Path);
        await next(context);
    }

    private static bool ValidHeader(HttpRequest request, PathAndQuery unencodedUrl)
    {
        var encodedPath = Regex.Replace(WebUtility.UrlDecode(unencodedUrl.Path), "/+", "/");

        return string.Equals(request.Path, encodedPath, StringComparison.Ordinal)
            && string.Equals(request.QueryString.ToString(), unencodedUrl.Query, StringComparison.Ordinal);
    }

    private static bool TryGetUnencodedUrlFromHeader(HttpRequest request, out PathAndQuery unencodedUrl)
    {
        if (!request.Headers.TryGetValue(UnencodedUrlHeaderName, out var unencodedUrlHeader) || !unencodedUrlHeader.Any())
        {
            unencodedUrl = new PathAndQuery("", "");
            return false;
        }

        var rawUnencodedUrl = unencodedUrlHeader.First();

        if (rawUnencodedUrl == null)
        {
            unencodedUrl = new PathAndQuery("", "");
        }
        else if (!rawUnencodedUrl.Contains("?"))
        {
            unencodedUrl = new PathAndQuery(rawUnencodedUrl, "");
        }
        else
        {
            var parts = rawUnencodedUrl.Split("?", 2);
            unencodedUrl = new PathAndQuery(parts[0], "?" + parts[1]);
        }

        return true;
    }

    private record PathAndQuery(string Path, string Query)
    {
        public override string ToString() => $"{Path}{Query}";
    }
}
