using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogOtter.HttpPatch.Tests.Api;
using LogOtter.HttpPatch.Tests.Api.Controllers;
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
            ImmutableList<string>.Empty);

    [Theory]
    [InlineData(SerializationEngine.Newtonsoft)]
    [InlineData(SerializationEngine.SystemText)]
    public async Task CanPatchName(SerializationEngine serializationEngine)
    {
        await using var testApi = new TestApi(serializationEngine);
        using var client = testApi.CreateClient();

        testApi.DataStore.UpsertResource(InitialState);

        var response = await client.PatchAsJsonAsync("/test-resource/12345", new { name = "Bob Bobertson" });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.Name.Should().Be("Bob Bobertson", "The patched value should be updated");
        updatedResource.Description.Should().Be("Bobertson family patriarch", "Values that aren't patched should be unaltered");
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

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.Count.Should().Be(10, "The patched value should be updated");
        updatedResource.Name.Should().Be("Bob", "Values that aren't patched should be unaltered");
        updatedResource.Description.Should().Be("Bobertson family patriarch", "Values that aren't patched should be unaltered");
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
            new { address = new { line1 = "Centenary Plaza", postCode = "B1 1TB" } });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");

        updatedResource.Count.Should().Be(0, "The patched value should be updated");
        updatedResource.Name.Should().Be("Bob", "Values that aren't patched should be unaltered");
        updatedResource.Description.Should().Be("Bobertson family patriarch", "Values that aren't patched should be unaltered");

        updatedResource.Address.Should().BeEquivalentTo(new { Line1 = "Centenary Plaza", Postcode = "B1 1TB" });
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

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.Description.Should().Be("A fake Bobertson", "The patched value should be updated");
        updatedResource.Name.Should().Be("Bob", "Values that aren't patched should be unaltered");
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

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.Description.Should().BeNull("The patched value should be updated");
        updatedResource.Name.Should().Be("Bob", "Values that aren't patched should be unaltered");
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

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
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
            new { address = new { line1 = (string?)null, postCode = (string?)null } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
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

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
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

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
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

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.State.Should().Be(ResourceState.Published, "The patched value should be updated");
        updatedResource.Description.Should().Be("Bobertson family patriarch", "Values that aren't patched should be unaltered");
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

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updatedResource = testApi.DataStore.GetResource("12345");
        updatedResource.People.Should().BeEquivalentTo(new[] { "Bob", "Bobetta" }, "The patched value should be updated");
        updatedResource.Description.Should().Be("Bobertson family patriarch", "Values that aren't patched should be unaltered");
    }
}
