using System.Net;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Shouldly;
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
            },
            cancellationToken: TestContext.Current.CancellationToken
        );
        await containerMock.CreateItemAsync(
            new TestClass
            {
                Id = "Foo2",
                PartitionKey = "Group1",
                MyValue = "Bar2",
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        var batch = containerMock.CreateTransactionalBatch(new PartitionKey("Group1")).ReadItem("Foo1").ReadItem("Foo2");

        var response = await batch.ExecuteAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var foo1Result = response.GetOperationResultAtIndex<TestClass>(0);
        foo1Result.ShouldNotBeNull();
        foo1Result.Resource.Id.ShouldBe("Foo1");
        foo1Result.Resource.MyValue.ShouldBe("Bar1");

        var foo2Result = response.GetOperationResultAtIndex<TestClass>(1);
        foo2Result.ShouldNotBeNull();
        foo2Result.Resource.Id.ShouldBe("Foo2");
        foo2Result.Resource.MyValue.ShouldBe("Bar2");
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
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        var batch = containerMock.CreateTransactionalBatch(new PartitionKey("Group1")).ReadItem("Foo1").ReadItem("Foo2");

        var response = await batch.ExecuteAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
