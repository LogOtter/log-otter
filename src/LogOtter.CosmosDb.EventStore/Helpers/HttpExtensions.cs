using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore;

internal static class HttpExtensions
{
    public static string GetHost(this HttpRequest request)
    {
        var host = request.Host.Value;

        var length = request.Scheme.Length + 3 + host.Length;

        return new StringBuilder(length)
            .Append(request.Scheme)
            .Append("://")
            .Append(host)
            .ToString();
    }

    public static int GetPage(this HttpRequest request)
    {
        var pageQueryStrings = request.Query["page"];

        foreach (var pageQueryString in pageQueryStrings)
        {
            if (int.TryParse(pageQueryString, out var page))
            {
                return page;
            }
        }

        return 1;
    }

    public static bool EndsWithSlash(this PathString path)
    {
        return path.HasValue && path.Value.EndsWith("/", StringComparison.Ordinal);
    }

    public static PathString EnsurePathDoesNotEndWithSlash(this PathString path)
    {
        if (path.EndsWithSlash())
        {
            return new PathString(path.Value!.TrimEnd('/'));
        }
        return path;
    }

}
