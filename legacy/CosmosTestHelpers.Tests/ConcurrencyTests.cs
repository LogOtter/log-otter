using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace CosmosTestHelpers.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task CanSimulateAConcurrencyException()
    {
        var sut = new ContainerMock(partitionKeyPath: "/partitionKey");

        await sut.UpsertItemAsync(new TestClass { Id = "MyId", PartitionKey = "APartition", MyValue = "Value1" }, new PartitionKey("APartition"));
        sut.TheNextWriteToDocumentRequiresEtagAndWillRaiseAConcurrencyException(new PartitionKey("APartition"), "MyId");
            
        var documentInDb = await sut.ReadItemAsync<TestClass>("MyId", new PartitionKey("APartition"));

        Func<Task> mustPassETag = () => sut.UpsertItemAsync(new TestClass { Id = "MyId", PartitionKey = "APartition", MyValue = "Value2" }, new PartitionKey("APartition"));
        (await mustPassETag.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Be("An eTag must be provided as a concurrency exception is queued");

        // First update should fail as if the document has been modified by another process
        Func<Task> shouldPreConditionFail = () => sut.UpsertItemAsync(new TestClass { Id = "MyId", PartitionKey = "APartition", MyValue = "Value2" }, new PartitionKey("APartition"), requestOptions: new ItemRequestOptions { IfMatchEtag = documentInDb.ETag });
        (await shouldPreConditionFail.Should().ThrowAsync<CosmosException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);

        // Second update should succeed if it has the correct etag
        documentInDb = await sut.ReadItemAsync<TestClass>("MyId", new PartitionKey("APartition"));
        var success = await sut.UpsertItemAsync(new TestClass { Id = "MyId", PartitionKey = "APartition", MyValue = "Value2" }, new PartitionKey("APartition"), requestOptions: new ItemRequestOptions { IfMatchEtag = documentInDb.ETag });
        success.StatusCode.Should().Be(HttpStatusCode.OK);
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