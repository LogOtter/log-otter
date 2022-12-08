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
}