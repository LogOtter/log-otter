﻿using System.Net;
using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.ContainerMockData;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        containerMock.GetAllItems<TestClass>().Should().HaveCount(2);

        var foo1 = await containerMock.ReadItemAsync<TestClass>("Foo1", new PartitionKey("Group1"));
        var foo2 = await containerMock.ReadItemAsync<TestClass>("Foo2", new PartitionKey("Group1"));

        foo1.Should().NotBeNull();
        foo1.Resource.MyValue.Should().Be("Bar1");

        foo2.Should().NotBeNull();
        foo2.Resource.MyValue.Should().Be("Bar2");
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

        containerMock.GetAllItems<TestClass>().Should().HaveCount(0);

        var action1 = async () => await containerMock.ReadItemAsync<TestClass>("Foo1", new PartitionKey("Group1"));
        var action2 = async () => await containerMock.ReadItemAsync<TestClass>("Foo2", new PartitionKey("Group1"));

        (await action1.Should().ThrowAsync<CosmosException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await action2.Should().ThrowAsync<CosmosException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        containerMock.GetAllItems<TestClass>().Should().HaveCount(0);
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
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatingItemsInBatchExecutesDataChangedEvents()
    {
        var containerMock = new ContainerMock();
        bool dataChangeCalled = false;
        containerMock.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Created);
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

        dataChangeCalled.Should().Be(false);

        var response = await batch.ExecuteAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        dataChangeCalled.Should().Be(true);
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
