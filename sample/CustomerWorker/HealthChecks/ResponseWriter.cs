using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LogOtter.SimpleHealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CustomerWorker.HealthChecks;

internal static class ResponseWriter
{
    private static readonly JsonSerializerOptions JsonSerializerOptions;

    static ResponseWriter()
    {
        JsonSerializerOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public static async Task WriteDetailedJson(IHttpListenerContext context, HealthReport report)
    {
        var response = context.Response;

        response.ContentType = "application/json; charset=utf-8";

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(report, JsonSerializerOptions));

        await response.OutputStream.WriteAsync(body);
    }
}
