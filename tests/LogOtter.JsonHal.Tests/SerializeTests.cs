using System.Text.Json;
using Shouldly;
using Xunit;

namespace LogOtter.JsonHal.Tests;

public class SerializeTests
{
    [Fact]
    public void Serialize_SelfOnly()
    {
        var linkCollection = new JsonHalLinkCollection();
        linkCollection.AddSelfLink("/foo/self");

        var json = JsonSerializer.Serialize(linkCollection);

        json.ShouldBe("""{"self":{"href":"/foo/self"}}""");
    }

    [Fact]
    public void Serialize_NextOnly()
    {
        var linkCollection = new JsonHalLinkCollection();
        linkCollection.AddNextLink("/foo/next");

        var json = JsonSerializer.Serialize(linkCollection);

        json.ShouldBe("""{"next":{"href":"/foo/next"}}""");
    }

    [Fact]
    public void Serialize_SelfAndNext()
    {
        var linkCollection = new JsonHalLinkCollection();
        linkCollection.AddSelfLink("/foo/self");
        linkCollection.AddNextLink("/foo/next");

        var json = JsonSerializer.Serialize(linkCollection);

        json.ShouldBe("""{"self":{"href":"/foo/self"},"next":{"href":"/foo/next"}}""");
    }

    [Fact]
    public void Serialize_MultiLinkType()
    {
        var linkCollection = new JsonHalLinkCollection();
        linkCollection.AddLink("related", "/foo/related1");
        linkCollection.AddLink("related", "/foo/related2");

        var json = JsonSerializer.Serialize(linkCollection);

        json.ShouldBe("""{"related":[{"href":"/foo/related1"},{"href":"/foo/related2"}]}""");
    }
}
