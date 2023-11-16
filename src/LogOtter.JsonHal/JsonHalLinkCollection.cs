using System.Collections;
using System.Text.Json.Serialization;

namespace LogOtter.JsonHal;

[JsonConverter(typeof(JsonHalLinkCollectionConverter))]
public sealed class JsonHalLinkCollection : IReadOnlyCollection<JsonHalLink>
{
    private readonly List<JsonHalLink> _links = new();

    public int Count => _links.Count;

    public IEnumerator<JsonHalLink> GetEnumerator()
    {
        return _links.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_links).GetEnumerator();
    }

    public JsonHalLink? GetLink(string type)
    {
        return _links.SingleOrDefault(l => l.Type == type);
    }

    public IReadOnlyCollection<JsonHalLink> GetLinks(string type)
    {
        return _links.Where(l => l.Type == type).ToList();
    }

    public void AddLink(string type, string href)
    {
        AddLink(new JsonHalLink(type, href));
    }

    private void AddLink(JsonHalLink link)
    {
        _links.Add(link);
    }
}
