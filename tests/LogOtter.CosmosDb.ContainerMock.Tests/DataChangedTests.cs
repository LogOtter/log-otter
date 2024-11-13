using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.ContainerMockData;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests;

public class DataChangedTests
{
    #region Items
    [Fact]
    public async Task CreatingItemInContainerInvokesChangedEvent()
    {
        var sut = new ContainerMock();
        bool dataChangeCalled = false;
        sut.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Created);
        };
        await sut.CreateItemAsync(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value1",
            },
            new PartitionKey("APartition")
        );

        dataChangeCalled.Should().Be(true);
    }

    [Fact]
    public async Task UpsertingNewItemToContainerInvokesChangedEvent()
    {
        var sut = new ContainerMock();
        bool dataChangeCalled = false;
        sut.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Created);
        };
        await sut.UpsertItemAsync(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value1",
            },
            new PartitionKey("APartition")
        );

        dataChangeCalled.Should().Be(true);
    }

    [Fact]
    public async Task ReplaceItemInContainerInvokesChangeEvent()
    {
        var sut = new ContainerMock();
        await sut.UpsertItemAsync(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value1",
            },
            new PartitionKey("APartition")
        );
        bool dataChangeCalled = false;
        sut.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Updated);
        };
        await sut.ReplaceItemAsync(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value2",
            },
            "MyId",
            new PartitionKey("APartition")
        );

        dataChangeCalled.Should().Be(true);
    }

    [Fact]
    public async Task UpsertingExistingItemToContainerInvokesChangedEvent()
    {
        var sut = new ContainerMock();
        await sut.UpsertItemAsync(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value1",
            },
            new PartitionKey("APartition")
        );

        bool dataChangeCalled = false;
        sut.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Updated);
        };
        await sut.UpsertItemAsync(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value2",
            },
            new PartitionKey("APartition")
        );

        dataChangeCalled.Should().Be(true);
    }

    [Fact]
    public async Task DeletingItemFromContainerInvokesChangedEvent()
    {
        var sut = new ContainerMock();
        await sut.UpsertItemAsync(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value1",
            },
            new PartitionKey("APartition")
        );

        bool dataChangeCalled = false;
        sut.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Deleted);
        };
        await sut.DeleteItemAsync<TestClass>("MyId", new PartitionKey("APartition"));

        dataChangeCalled.Should().Be(true);
    }

    #endregion
    #region Stream

    [Fact]
    public async Task CreatingItemStreamInContainerInvokesChangedEvent()
    {
        var sut = new ContainerMock();
        bool dataChangeCalled = false;
        sut.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Created);
        };
        var item = new TestClass()
        {
            Id = "MyId",
            PartitionKey = "APartition",
            MyValue = "Value1",
        };
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
        await using var ms = new MemoryStream(bytes);
        await sut.CreateItemStreamAsync(ms, new PartitionKey("APartition"));

        dataChangeCalled.Should().Be(true);
    }

    [Fact]
    public async Task UpsertingNewItemStreamToContainerInvokesChangedEvent()
    {
        var sut = new ContainerMock();
        bool dataChangeCalled = false;
        sut.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Created);
        };
        var item = new TestClass()
        {
            Id = "MyId",
            PartitionKey = "APartition",
            MyValue = "Value1",
        };
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
        await using var ms = new MemoryStream(bytes);
        await sut.UpsertItemStreamAsync(ms, new PartitionKey("APartition"));

        dataChangeCalled.Should().Be(true);
    }

    [Fact]
    public async Task UpsertingExistingItemStreamToContainerInvokesChangedEvent()
    {
        var sut = new ContainerMock();
        var item = new TestClass()
        {
            Id = "MyId",
            PartitionKey = "APartition",
            MyValue = "Value1",
        };
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
        await using var ms = new MemoryStream(bytes);
        await sut.UpsertItemStreamAsync(ms, new PartitionKey("APartition"));

        bool dataChangeCalled = false;
        sut.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Updated);
        };
        item.MyValue = "Value2";
        bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
        await using var ms2 = new MemoryStream(bytes);
        await sut.UpsertItemStreamAsync(ms2, new PartitionKey("APartition"));

        dataChangeCalled.Should().Be(true);
    }

    [Fact]
    public async Task DeletingItemStreamFromContainerInvokesChangedEvent()
    {
        var sut = new ContainerMock();
        var item = new TestClass()
        {
            Id = "MyId",
            PartitionKey = "APartition",
            MyValue = "Value1",
        };
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
        await using var ms = new MemoryStream(bytes);
        await sut.UpsertItemStreamAsync(ms, new PartitionKey("APartition"));

        bool dataChangeCalled = false;
        sut.DataChanged += (_, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Deleted);
        };
        await sut.DeleteItemStreamAsync("MyId", new PartitionKey("APartition"));

        dataChangeCalled.Should().Be(true);
    }

    #endregion
    #region Batch
    [Fact]
    public async Task ItemChangesCalledInBatchArentProcessedUntillBatchIsExecuted()
    {
        var sut = new ContainerMock();
        List<DataChangedEventArgs> dataChanges = new();
        sut.DataChanged += (_, args) =>
        {
            args.Should().NotBeNull();
            dataChanges.Add(args);
        };
        await sut.CreateItemAsync(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value1",
            },
            new PartitionKey("APartition"),
            null,
            DataChangeMode.Batch
        );
        await sut.UpsertItemAsync(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value2",
            },
            new PartitionKey("APartition"),
            null,
            DataChangeMode.Batch
        );
        await sut.DeleteItemAsync<TestClass>("MyId", new PartitionKey("APartition"), null, DataChangeMode.Batch);

        dataChanges.Count.Should().Be(0);

        sut.ExecuteDataChanges();

        dataChanges.Count.Should().Be(3);
        dataChanges[0].Operation.Should().Be(Operation.Created);
        dataChanges[1].Operation.Should().Be(Operation.Updated);
        dataChanges[2].Operation.Should().Be(Operation.Deleted);
    }

    [Fact]
    public async Task StreamChangesCalledInBatchArentProcessedUntillBatchIsExecuted()
    {
        var sut = new ContainerMock();
        List<DataChangedEventArgs> dataChanges = new();
        sut.DataChanged += (_, args) =>
        {
            args.Should().NotBeNull();
            dataChanges.Add(args);
        };
        var createItemBytes = System.Text.Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new TestClass()
                {
                    Id = "MyId",
                    PartitionKey = "APartition",
                    MyValue = "Value1",
                }
            )
        );
        await using var createStream = new MemoryStream(createItemBytes);
        await sut.CreateItemStreamAsync(createStream, new PartitionKey("APartition"), null, DataChangeMode.Batch);

        var updateBytes = System.Text.Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                new TestClass()
                {
                    Id = "MyId",
                    PartitionKey = "APartition",
                    MyValue = "Value2",
                }
            )
        );
        await using var updateStream = new MemoryStream(updateBytes);

        await sut.UpsertItemStreamAsync(updateStream, new PartitionKey("APartition"), null, DataChangeMode.Batch);

        await sut.DeleteItemStreamAsync("MyId", new PartitionKey("APartition"), null, DataChangeMode.Batch);

        dataChanges.Count.Should().Be(0);

        sut.ExecuteDataChanges();

        dataChanges.Count.Should().Be(3);
        dataChanges[0].Operation.Should().Be(Operation.Created);
        dataChanges[1].Operation.Should().Be(Operation.Updated);
        dataChanges[2].Operation.Should().Be(Operation.Deleted);
    }
    #endregion






    private class TestClass
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; } = null!;

        public string? MyValue { get; set; }
    }
}
