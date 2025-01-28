using System.Text.Json;
using Shouldly;
using Xunit;

namespace LogOtter.JsonHal.Tests;

public class DeserializeTests
{
    [Fact]
    public void Deserialize_NextOnly()
    {
        var json = """
            {
                "next": {
                    "href": "/foo/next"
                }
            }
            """;

        var linkCollection = JsonSerializer.Deserialize<JsonHalLinkCollection>(json);

        linkCollection.ShouldNotBeNull();
        linkCollection.Count.ShouldBe(1);

        linkCollection.ShouldContain(new JsonHalLink("next", "/foo/next"));
    }

    [Fact]
    public void Deserialize_SelfOnly()
    {
        var json = """
            {
                "self": {
                    "href": "/foo/self"
                }
            }
            """;

        var linkCollection = JsonSerializer.Deserialize<JsonHalLinkCollection>(json);

        linkCollection.ShouldNotBeNull();
        linkCollection.Count.ShouldBe(1);

        linkCollection.ShouldContain(new JsonHalLink("self", "/foo/self"));
    }

    [Fact]
    public void Deserialize_SelfAndNext()
    {
        var json = """
            {
                "self": {
                    "href": "/foo/self"
                },
                "next": {
                    "href": "/foo/next"
                }
            }
            """;

        var linkCollection = JsonSerializer.Deserialize<JsonHalLinkCollection>(json);

        linkCollection.ShouldNotBeNull();
        linkCollection.Count.ShouldBe(2);

        linkCollection.ShouldContain(new JsonHalLink("self", "/foo/self"));
        linkCollection.ShouldContain(new JsonHalLink("next", "/foo/next"));
    }

    [Fact]
    public void Deserialize_MultiLinkType()
    {
        var json = """
            {
                "related": [{
                    "href": "/foo/related1"
                }, {
                    "href": "/foo/related2"
                }]
            }
            """;

        var linkCollection = JsonSerializer.Deserialize<JsonHalLinkCollection>(json);

        linkCollection.ShouldNotBeNull();
        linkCollection.Count.ShouldBe(2);

        linkCollection.ShouldContain(new JsonHalLink("related", "/foo/related1"));
    }
}
