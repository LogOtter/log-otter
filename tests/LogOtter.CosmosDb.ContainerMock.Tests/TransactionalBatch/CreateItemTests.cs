using System.Net;
using LogOtter.CosmosDb.ContainerMock.ContainerMockData;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests.TransactionalBatch;

public class CreateItemTests
{
    [Fact]
    public async Task CreateItem()
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
            );

        var response = await batch.ExecuteAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        containerMock.GetAllItems<TestClass>().Count().ShouldBe(2);

        var foo1 = await containerMock.ReadItemAsync<TestClass>("Foo1", new PartitionKey("Group1"));
        var foo2 = await containerMock.ReadItemAsync<TestClass>("Foo2", new PartitionKey("Group1"));

        foo1.ShouldNotBeNull();
        foo1.Resource.MyValue.ShouldBe("Bar1");

        foo2.ShouldNotBeNull();
        foo2.Resource.MyValue.ShouldBe("Bar2");
    }

    [Fact]
    public async Task CreateItem_NoExecute_NoChanges()
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
            );

        // Intentionally not calling: await batch.ExecuteAsync();

        containerMock.GetAllItems<TestClass>().Count().ShouldBe(0);

        var action1 = async () => await containerMock.ReadItemAsync<TestClass>("Foo1", new PartitionKey("Group1"));
        var action2 = async () => await containerMock.ReadItemAsync<TestClass>("Foo2", new PartitionKey("Group1"));

        (await action1.ShouldThrowAsync<CosmosException>()).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await action2.ShouldThrowAsync<CosmosException>()).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateItem_ExecuteFails_Rollback()
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
                    Id = "INVALID#ID",
                    PartitionKey = "Group1",
                    MyValue = "Bar2",
                }
            );

        var response = await batch.ExecuteAsync();
        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);

        containerMock.GetAllItems<TestClass>().Count().ShouldBe(0);
    }

    [Fact]
    public async Task OriginalCosmosExceptionIsSurfaced()
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
            );

        containerMock.QueueExceptionToBeThrown(
            new CosmosException("Conflict, oh no!", HttpStatusCode.Conflict, 0, string.Empty, 0),
            i => i.MethodName == "CreateItemStreamAsync"
        );

        var response = await batch.ExecuteAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatingItemsInBatchExecutesDataChangedEvents()
    {
        var containerMock = new ContainerMock();
        bool dataChangeCalled = false;
        containerMock.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.ShouldNotBeNull();
            args.Operation.ShouldBe(Operation.Created);
        };

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
            );

        dataChangeCalled.ShouldBe(false);

        var response = await batch.ExecuteAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        dataChangeCalled.ShouldBe(true);
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
