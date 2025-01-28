using System.Net;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests.TransactionalBatch;

public class CreateReadTests
{
    [Fact]
    public async Task CreateItem_ExecuteFails_Rollback_Empty()
    {
        var containerMock = new ContainerMock();

        var batch = containerMock
            .CreateTransactionalBatch(new PartitionKey("Group1"))
            .CreateItem(
                new TestClass
                {
                    Id = "Foo1",
                    PartitionKey = "Group1",
                    MyValue = "Bar1",
                }
            )
            .CreateItem(
                new TestClass
                {
                    Id = "Foo2",
                    PartitionKey = "Group1",
                    MyValue = "Bar2",
                }
            )
            .ReadItem("DoesNotExist");

        var response = await batch.ExecuteAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        containerMock.GetAllItems<TestClass>().Count().ShouldBe(0);
    }

    [Fact]
    public async Task CreateItem_ExecuteFails_Rollback()
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

        var batch = containerMock
            .CreateTransactionalBatch(new PartitionKey("Group1"))
            .CreateItem(
                new TestClass
                {
                    Id = "Foo2",
                    PartitionKey = "Group1",
                    MyValue = "Bar2",
                }
            )
            .CreateItem(
                new TestClass
                {
                    Id = "Foo3",
                    PartitionKey = "Group1",
                    MyValue = "Bar3",
                }
            )
            .ReadItem("DoesNotExist");

        var response = await batch.ExecuteAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var allItems = containerMock.GetAllItems<TestClass>().ToList();
        allItems.Count.ShouldBe(1);

        var item1 = allItems.Single().Document;
        item1.Id.ShouldBe("Foo1");
        item1.MyValue.ShouldBe("Bar1");
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
