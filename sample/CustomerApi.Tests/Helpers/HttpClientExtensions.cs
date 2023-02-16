using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

// ReSharper disable CheckNamespace
namespace System.Net.Http.Json;

public static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> PatchAsJObjectAsync(
        this HttpClient client,
        string? requestUri,
        JsonObject request,
        CancellationToken cancellationToken = default
    )
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var mediaType = new MediaTypeHeaderValue("application/json");
        var content = new StringContent(request.ToJsonString(), Encoding.UTF8, mediaType.ToString());

        return client.PatchAsync(requestUri, content, cancellationToken);
    }
}
