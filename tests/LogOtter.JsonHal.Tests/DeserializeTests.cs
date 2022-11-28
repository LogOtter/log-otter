using System.Text.Json;
using FluentAssertions;
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

        linkCollection.Should().NotBeNull();
        linkCollection.Should().HaveCount(1);
        
        linkCollection.Should().Contain(new JsonHalLink("next", "/foo/next"));
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

        linkCollection.Should().NotBeNull();
        linkCollection.Should().HaveCount(1);

        linkCollection.Should().Contain(new JsonHalLink("self", "/foo/self"));
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

        linkCollection.Should().NotBeNull();
        linkCollection.Should().HaveCount(2);

        linkCollection.Should().Contain(new JsonHalLink("self", "/foo/self"));
        linkCollection.Should().Contain(new JsonHalLink("next", "/foo/next"));
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

        linkCollection.Should().NotBeNull();
        linkCollection.Should().HaveCount(2);

        linkCollection.Should().Contain(new JsonHalLink("related", "/foo/related1"));
    }
}