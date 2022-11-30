# JsonHal

A library to help build JsonHal links.

## Usage

Add a links collection to your response object.

> Note: Ensure you set the `[JsonPropertyName]` attribute to `_links`

```c#
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

public class ExampleResponse
{
    public required IReadOnlyCollection<string> Data { get; init; }

    [JsonPropertyName("_links")]
    public required JsonHalLinkCollection Links { get; init; }

    [SetsRequiredMembers]
    public ExampleResponse(IReadOnlyCollection<string> data)
    {
        Data = data;
        Links = new JsonHalLinkCollection();
    }

    public ExampleResponse()
    {
    }
}
```

Build your response using the helper methods.

```c#
var response = new ExampleResponse(data);
response.Links.AddFirstLink("https://example.com/data?page=1");
response.Links.AddPrevLink("https://example.com/data?page=2");
response.Links.AddSelfLink("https://example.com/data?page=3");
response.Links.AddNextLink("https://example.com/data?page=4");
response.Links.AddLastLink("https://example.com/data?page=5");
```

For paged responses, use the helper to generate all links (`first`, `prev`, `self`, `next`, `last`):

```c#
response.Links.AddPagedLinks(currentPage, totalPages, p => $"https://example.com/data?page={p}");
```

Use ASP.NET `Url` helper for help crafting full URLs, e.g.

```c#
response.Links.AddSelfLink(Url.Link(
    "RouteName",                // replace with the name of the route
    new { page = currentPage }  // replace with route parameters 
));
```

## More Information

See [HAL - Hypertext Application Language](https://stateless.group/hal_specification.html) for the specification