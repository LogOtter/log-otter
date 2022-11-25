namespace LogOtter.HttpPatch.Tests;

internal static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(this HttpClient client, string requestUri, T body)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Patch,
            Content = new JsonContent<T>(body),
            RequestUri = new Uri(requestUri, UriKind.RelativeOrAbsolute)
        };

        return client.SendAsync(httpRequestMessage);
    }
}