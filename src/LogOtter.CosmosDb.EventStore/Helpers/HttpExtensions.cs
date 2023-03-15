using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace LogOtter.CosmosDb.EventStore;

internal static class HttpExtensions
{
    public static string GetHost(this HttpRequest request)
    {
        var host = request.Host.Value;

        var length = request.Scheme.Length + 3 + host.Length;

        return new StringBuilder(length).Append(request.Scheme).Append("://").Append(host).ToString();
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

    public static string? GetLogOtterHubPath(this HttpRequest request)
    {
        var scheme = request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        var hostAndPort = request.Headers["X-Forwarded-Host"].FirstOrDefault();
        var path = request.Headers["X-LogOtter-Hub-Path"].FirstOrDefault();

        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(hostAndPort) || string.IsNullOrEmpty(scheme))
        {
            return null;
        }

        return CreateUri(scheme, hostAndPort, path);
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

    private static string CreateUri(string scheme, string hostAndPort, string path)
    {
        var uriBuilder = new UriBuilder { Scheme = scheme, Path = path };

        if (hostAndPort.Contains(':'))
        {
            var parts = hostAndPort.Split(':', 2);
            uriBuilder.Host = parts[0];
            uriBuilder.Port = int.Parse(parts[1]);
        }
        else
        {
            uriBuilder.Host = hostAndPort;
        }

        return uriBuilder.ToString();
    }
}
