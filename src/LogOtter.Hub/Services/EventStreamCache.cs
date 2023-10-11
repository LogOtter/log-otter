using System.Text.Json.Serialization;
using LogOtter.Hub.Configuration;
using LogOtter.Hub.Models;
using LogOtter.JsonHal;

namespace LogOtter.Hub.Services;

public class EventStreamCache
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventStreamCache> _logger;
    private readonly ServicesOptions _options;
    private AsyncLazy<IReadOnlyCollection<EventStreamCacheItem>> _cache;

    public EventStreamCache(ServicesOptions options, HttpClient httpClient, ILogger<EventStreamCache> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
        _cache = new AsyncLazy<IReadOnlyCollection<EventStreamCacheItem>>(CreateCache);
    }

    public async Task<IReadOnlyCollection<EventStreamDefinition>> GetEventStreamDefinitions()
    {
        return (await _cache.Value).Select(c => c.EventStreamDefinition).ToList();
    }

    public async Task<EventStreamDefinition?> GetEventStreamDefinition(string eventStreamName)
    {
        return (await _cache.Value).FirstOrDefault(es => es.EventStreamDefinition.Name == eventStreamName)?.EventStreamDefinition;
    }

    public async Task<string?> GetServiceUrlFor(string eventStreamName)
    {
        return (await _cache.Value).FirstOrDefault(es => es.EventStreamDefinition.Name == eventStreamName)?.ServiceDefinition.Url;
    }

    private async Task<IReadOnlyCollection<EventStreamCacheItem>> CreateCache()
    {
        _logger.LogInformation("Populating Event Stream Definition cache from {ServiceCount} services", _options.Services.Count());

        var tasks = _options.Services.Select(GetEventStreamDefinitionsFor).ToList();

        await Task.WhenAll(tasks);

        var cache = tasks.SelectMany(t => t.Result).OrderBy(es => es.EventStreamDefinition.Name).ToList();

        _logger.LogInformation("Event Stream Definition cache populated successfully with {DefinitionCount} definitions", cache.Count);

        return cache;
    }

    private async Task<IEnumerable<EventStreamCacheItem>> GetEventStreamDefinitionsFor(ServiceDefinition service)
    {
        var definitions = new List<EventStreamCacheItem>();

        var url = $"{service.Url}/event-streams";

        do
        {
            var response = await _httpClient.GetFromJsonAsync<GetEventStreamsResponse>(url);
            if (response == null)
            {
                break;
            }

            var cacheItems = response.Definitions.Select(
                d => new EventStreamCacheItem(new EventStreamDefinition(d.Name, d.TypeName, service.Name), service)
            );

            definitions.AddRange(cacheItems);

            var relativeUrl = response.Links.GetNextHref();

            url = !string.IsNullOrEmpty(relativeUrl) ? $"{service.Url}{relativeUrl}" : null;
        } while (!string.IsNullOrEmpty(url));

        return definitions;
    }

    private record EventStreamCacheItem(EventStreamDefinition EventStreamDefinition, ServiceDefinition ServiceDefinition);

    private record GetEventStreamsResponse(
        IEnumerable<EventStreamDefinitionResponse> Definitions,
        [property: JsonPropertyName("_links")] JsonHalLinkCollection Links
    );

    private record EventStreamDefinitionResponse(string Name, string TypeName);
}
