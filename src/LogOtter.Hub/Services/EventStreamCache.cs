using System.Diagnostics.CodeAnalysis;
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
    private IReadOnlyCollection<EventStreamCacheItem>? _cache;

    public EventStreamCache(ServicesOptions options, HttpClient httpClient, ILogger<EventStreamCache> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
    }

    public async Task<IReadOnlyCollection<EventStreamDefinition>> GetEventStreamDefinitions()
    {
        await EnsureCache();
        return _cache.Select(c => c.EventStreamDefinition).ToList();
    }

    public async Task<EventStreamDefinition?> GetEventStreamDefinition(string eventStreamName)
    {
        await EnsureCache();

        return _cache.FirstOrDefault(es => es.EventStreamDefinition.Name == eventStreamName)?.EventStreamDefinition;
    }

    public async Task<string?> GetServiceUrlFor(string eventStreamName)
    {
        await EnsureCache();

        return _cache.FirstOrDefault(es => es.EventStreamDefinition.Name == eventStreamName)?.ServiceDefinition.Url;
    }

    [MemberNotNull(nameof(_cache))]
    private async Task EnsureCache()
    {
        if (_cache != null)
        {
            return;
        }

        _logger.LogInformation("Populating Event Stream Definition cache from {ServiceCount} services", _options.Services.Count());

        var tasks = _options.Services.Select(GetEventStreamDefinitionsFor).ToList();

        await Task.WhenAll(tasks);

        _cache = tasks.SelectMany(t => t.Result).OrderBy(es => es.EventStreamDefinition.Name).ToList();

        _logger.LogInformation("Event Stream Definition cache populated successfully with {DefinitionCount} definitions", _cache.Count);
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

            url = response.Links.GetNextHref();
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
