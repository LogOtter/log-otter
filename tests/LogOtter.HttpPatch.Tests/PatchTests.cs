using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using LogOtter.HttpPatch.Tests.Api;
using LogOtter.HttpPatch.Tests.Api.Controllers;
using Shouldly;
using Xunit;

namespace LogOtter.HttpPatch.Tests;

public class PatchTests
{
    private static TestResource InitialState =>
        new(
            "12345",
            "Bob",
            "Bobertson family patriarch",
            0,
            new Address("Alpha Tower", "B1 1TT"),
            ResourceState.Unpublished,
            ImmutableList<string>.Empty
        );

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CanPatchName(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync("/test-resource/12345", new { name = "Bob Bobertson" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.Name.ShouldBe("Bob Bobertson", "The patched value should be updated");
        updatedResource.Description.ShouldBe("Bobertson family patriarch", "Values that aren't patched should be unaltered");
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CanPatchCount(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync("/test-resource/12345", new { count = 10 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.Count.ShouldBe(10, "The patched value should be updated");
        updatedResource.Name.ShouldBe("Bob", "Values that aren't patched should be unaltered");
        updatedResource.Description.ShouldBe("Bobertson family patriarch", "Values that aren't patched should be unaltered");
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CanPatchAddress(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync(
            "/test-resource/12345",
            new { address = new { line1 = "Centenary Plaza", postCode = "B1 1TB" } }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");

        updatedResource.Count.ShouldBe(0, "The patched value should be updated");
        updatedResource.Name.ShouldBe("Bob", "Values that aren't patched should be unaltered");
        updatedResource.Description.ShouldBe("Bobertson family patriarch", "Values that aren't patched should be unaltered");

        updatedResource.Address.ShouldBeEquivalentTo(new { Line1 = "Centenary Plaza", Postcode = "B1 1TB" });
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CanPatchDescription(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync("/test-resource/12345", new { description = "A fake Bobertson" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.Description.ShouldBe("A fake Bobertson", "The patched value should be updated");
        updatedResource.Name.ShouldBe("Bob", "Values that aren't patched should be unaltered");
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CanPatchDescriptionToNull(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync("/test-resource/12345", new { description = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.Description.ShouldBeNull("The patched value should be updated");
        updatedResource.Name.ShouldBe("Bob", "Values that aren't patched should be unaltered");
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CantPatchPrimitiveToNull(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync("/test-resource/12345", new { count = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CantPatchWhenViolatingAttributesOnSubObject(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync(
            "/test-resource/12345",
            new { address = new { line1 = (string?)null, postCode = (string?)null } }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CantPatchNameToNull(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync("/test-resource/12345", new { name = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CantPatchNameToShort(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync("/test-resource/12345", new { name = "b" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CanPatchEnumUsingString(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync("/test-resource/12345", new { state = "Published" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.State.ShouldBe(ResourceState.Published, "The patched value should be updated");
        updatedResource.Description.ShouldBe("Bobertson family patriarch", "Values that aren't patched should be unaltered");
    }

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CanPatchPeople(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync("/test-resource/12345", new { people = new[] { "Bob", "Bobetta" } });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.People.ShouldBeEquivalentTo(new[] { "Bob", "Bobetta" }, "The patched value should be updated");
        updatedResource.Description.ShouldBe("Bobertson family patriarch", "Values that aren't patched should be unaltered");
    }
}
