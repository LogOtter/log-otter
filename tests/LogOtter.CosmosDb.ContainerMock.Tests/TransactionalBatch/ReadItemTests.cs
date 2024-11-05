using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests.TransactionalBatch;

public class ReadItemTests
{
    [Fact]
    public async Task ReadItem()
    {
        var containerMock = new ContainerMock();
        await containerMock.CreateItemAsync(
            new TestClass
            {
                Id = "Foo1",
                PartitionKey = "Group1",
                MyValue = "Bar1",
            }
        );
        await containerMock.CreateItemAsync(
            new TestClass
            {
                Id = "Foo2",
                PartitionKey = "Group1",
                MyValue = "Bar2",
            }
        );

        var batch = containerMock.CreateTransactionalBatch(new PartitionKey("Group1")).ReadItem("Foo1").ReadItem("Foo2");

        var response = await batch.ExecuteAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var foo1Result = response.GetOperationResultAtIndex<TestClass>(0);
        foo1Result.Should().NotBeNull();
        foo1Result.Resource.Id.Should().Be("Foo1");
        foo1Result.Resource.MyValue.Should().Be("Bar1");

        var foo2Result = response.GetOperationResultAtIndex<TestClass>(1);
        foo2Result.Should().NotBeNull();
        foo2Result.Resource.Id.Should().Be("Foo2");
        foo2Result.Resource.MyValue.Should().Be("Bar2");
    }

    [Fact]
    public async Task ReadItem_Fails()
    {
        var containerMock = new ContainerMock();
        await containerMock.CreateItemAsync(
            new TestClass
            {
                Id = "Foo1",
                PartitionKey = "Group1",
                MyValue = "Bar1",
            }
        );

        var batch = containerMock.CreateTransactionalBatch(new PartitionKey("Group1")).ReadItem("Foo1").ReadItem("Foo2");

        var response = await batch.ExecuteAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
