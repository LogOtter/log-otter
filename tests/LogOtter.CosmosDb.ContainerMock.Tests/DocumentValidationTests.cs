using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests;

public class DocumentValidationTests
{
    [Fact]
    public async Task CreatingItemWithPascalCaseIdThrowsBadRequestExplainingTheCasing()
    {
        var containerMock = new ContainerMock();

        var exception = await Should.ThrowAsync<CosmosException>(() =>
            containerMock.CreateItemAsync(
                new PascalCaseIdModel { Id = "1", PartitionKey = "pk" },
                new PartitionKey("pk"),
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        exception.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        exception.ResponseBody.ShouldBe(
            $"Cosmos DB requires documents to have an 'id' property, but the {nameof(PascalCaseIdModel)} document being written to container TestContainer doesn't have one. The document has a property called '{nameof(PascalCaseIdModel.Id)}', but property names are case sensitive, so make sure it's serialized as 'id', e.g. with [JsonProperty(\"id\")]."
        );
    }

    [Fact]
    public async Task CreatingItemWithoutIdThrowsBadRequestNamingItsType()
    {
        var containerMock = new ContainerMock();

        var exception = await Should.ThrowAsync<CosmosException>(() =>
            containerMock.CreateItemAsync(
                new IdlessModel { PartitionKey = "pk" },
                new PartitionKey("pk"),
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        exception.ResponseBody.ShouldBe(
            $"Cosmos DB requires documents to have an 'id' property, but the {nameof(IdlessModel)} document being written to container TestContainer doesn't have one. Make sure the {nameof(IdlessModel)} document has a property serialized as 'id', e.g. with [JsonProperty(\"id\")]."
        );
    }

    [Fact]
    public async Task CreatingItemStreamWithoutIdReturnsBadRequest()
    {
        var containerMock = new ContainerMock();

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"partitionKey\":\"pk\"}"));
        using var response = await containerMock.CreateItemStreamAsync(
            stream,
            new PartitionKey("pk"),
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.ErrorMessage.ShouldContain(
            "Cosmos DB requires documents to have an 'id' property, but the document being written to container TestContainer doesn't have one. Make sure the document has a property serialized as 'id', e.g. with [JsonProperty(\"id\")]."
        );
    }

    [Fact]
    public async Task CreatingItemWithPascalCasePartitionKeyThrowsBadRequestExplainingTheCasing()
    {
        var containerMock = new ContainerMock();

        var exception = await Should.ThrowAsync<CosmosException>(() =>
            containerMock.CreateItemAsync(
                new PascalCasePartitionKeyModel { Id = "1", PartitionKey = "pk" },
                new PartitionKey("pk"),
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        exception.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        exception.ResponseBody.ShouldBe(
            $"PartitionKey extracted from document doesn't match the one specified in the header. Learn more: https://aka.ms/CosmosDB/sql/errors/wrong-pk-value. Container TestContainer is partitioned by /partitionKey, but the {nameof(PascalCasePartitionKeyModel)} document doesn't have that property. The document has a property called '{nameof(PascalCasePartitionKeyModel.PartitionKey)}', but property names are case sensitive, so make sure it's serialized as 'partitionKey', e.g. with [JsonProperty(\"partitionKey\")]."
        );
    }

    [Fact]
    public async Task UpsertingItemWithDifferentPartitionKeyValueThrowsBadRequest()
    {
        var containerMock = new ContainerMock();

        var exception = await Should.ThrowAsync<CosmosException>(() =>
            containerMock.UpsertItemAsync(
                new TestModel { Id = "1", PartitionKey = "a" },
                new PartitionKey("b"),
                cancellationToken: TestContext.Current.CancellationToken
            )
        );

        exception.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        exception.ResponseBody.ShouldBe(
            $"PartitionKey extracted from document doesn't match the one specified in the header. Learn more: https://aka.ms/CosmosDB/sql/errors/wrong-pk-value. Container TestContainer is partitioned by /partitionKey, but the {nameof(TestModel)} document has the value [\"a\"] there, which doesn't match the partition key passed in ([\"b\"])."
        );
    }

    [Fact]
    public async Task CreatingItemWithoutPartitionKeyPropertyUsingPartitionKeyNoneStoresIt()
    {
        var containerMock = new ContainerMock();

        await containerMock.CreateItemAsync(
            new IdOnlyModel { Id = "1" },
            PartitionKey.None,
            cancellationToken: TestContext.Current.CancellationToken
        );

        containerMock.GetAllItems<IdOnlyModel>().ShouldHaveSingleItem().Document.Id.ShouldBe("1");
    }

    private class TestModel
    {
        [JsonProperty("id")]
        public required string Id { get; init; }

        [JsonProperty("partitionKey")]
        public required string PartitionKey { get; init; }
    }

    private class PascalCaseIdModel
    {
        public required string Id { get; init; }

        [JsonProperty("partitionKey")]
        public required string PartitionKey { get; init; }
    }

    private class PascalCasePartitionKeyModel
    {
        [JsonProperty("id")]
        public required string Id { get; init; }

        public required string PartitionKey { get; init; }
    }

    private class IdlessModel
    {
        [JsonProperty("partitionKey")]
        public required string PartitionKey { get; init; }
    }

    private class IdOnlyModel
    {
        [JsonProperty("id")]
        public required string Id { get; init; }
    }
}
