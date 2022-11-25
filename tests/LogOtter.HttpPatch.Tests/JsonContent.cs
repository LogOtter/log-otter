using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LogOtter.HttpPatch.Tests;

internal class JsonContent<T> : HttpContent
{
    private readonly T _content;

    public JsonContent(T content) {
        _content = content;
        Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return JsonSerializer.SerializeAsync(stream, _content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }
}