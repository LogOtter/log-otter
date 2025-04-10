using System.Net;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task CanSimulateAConcurrencyException()
    {
        var sut = new ContainerMock();

        await sut.UpsertItemAsync(
            new TestClass
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value1",
            },
            new PartitionKey("APartition"),
            cancellationToken: TestContext.Current.CancellationToken
        );
        sut.TheNextWriteToDocumentRequiresEtagAndWillRaiseAConcurrencyException(new PartitionKey("APartition"), "MyId");

        var documentInDb = await sut.ReadItemAsync<TestClass>(
            "MyId",
            new PartitionKey("APartition"),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Func<Task> mustPassETag = () =>
            sut.UpsertItemAsync(
                new TestClass
                {
                    Id = "MyId",
                    PartitionKey = "APartition",
                    MyValue = "Value2",
                },
                new PartitionKey("APartition"),
                cancellationToken: TestContext.Current.CancellationToken
            );
        (await mustPassETag.ShouldThrowAsync<InvalidOperationException>()).Message.ShouldBe(
            "An eTag must be provided as a concurrency exception is queued"
        );

        // First update should fail as if the document has been modified by another process
        Func<Task> shouldPreConditionFail = () =>
            sut.UpsertItemAsync(
                new TestClass
                {
                    Id = "MyId",
                    PartitionKey = "APartition",
                    MyValue = "Value2",
                },
                new PartitionKey("APartition"),
                new ItemRequestOptions { IfMatchEtag = documentInDb.ETag },
                cancellationToken: TestContext.Current.CancellationToken
            );
        (await shouldPreConditionFail.ShouldThrowAsync<CosmosException>()).StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);

        // Second update should succeed if it has the correct etag
        documentInDb = await sut.ReadItemAsync<TestClass>(
            "MyId",
            new PartitionKey("APartition"),
            cancellationToken: TestContext.Current.CancellationToken
        );
        var success = await sut.UpsertItemAsync(
            new TestClass
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value2",
            },
            new PartitionKey("APartition"),
            new ItemRequestOptions { IfMatchEtag = documentInDb.ETag },
            cancellationToken: TestContext.Current.CancellationToken
        );
        success.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private class TestClass
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; } = null!;

        public string? MyValue { get; set; }
    }
}
