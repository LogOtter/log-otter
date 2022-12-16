using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore;

internal static class HttpExtensions
{
    public static async Task WriteJsonAsync<T>(
        this HttpResponse response, 
        T value,
        JsonSerializerOptions? jsonSerializerOptions = null,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json;charset=utf-8";
        
        var json = JsonSerializer.Serialize(value, jsonSerializerOptions);

        await response.WriteAsync(json, new UTF8Encoding(false));
    }

    public static void Redirect(this HttpResponse response, string location)
    {
        response.StatusCode = (int)HttpStatusCode.Redirect;
        response.Headers.Location = location;
    } 
    
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