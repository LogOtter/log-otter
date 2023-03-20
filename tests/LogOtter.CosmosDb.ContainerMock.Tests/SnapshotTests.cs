using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests;

public class SnapshotTests
{
    [Fact]
    public async Task RestoringAnEmptySnapshotResetsData()
    {
        var container = new ContainerMock();

        var snapshot = container.CreateSnapshot();

        var item = new TestClass
        {
            Id = "Foo",
            PartitionKey = "Bar",
            MyValue = "FooBar"
        };
        await container.CreateItemAsync(item, new PartitionKey("Bar"));

        container.RestoreSnapshot(snapshot);

        var itemCount = await container.CountAsync<TestClass>("Bar", q => q);
        itemCount.Should().Be(0);
    }

    [Fact]
    public async Task RestoringASnapshotDeletesData()
    {
        var container = new ContainerMock();

        var item1 = new TestClass
        {
            Id = "Foo",
            PartitionKey = "Bar",
            MyValue = "FooBar"
        };
        await container.CreateItemAsync(item1, new PartitionKey("Bar"));

        var snapshot = container.CreateSnapshot();

        var item2 = new TestClass
        {
            Id = "Foo2",
            PartitionKey = "Bar",
            MyValue = "FooBar2"
        };
        await container.CreateItemAsync(item2, new PartitionKey("Bar"));

        container.RestoreSnapshot(snapshot);

        var itemCount = await container.CountAsync<TestClass>("Bar", q => q);
        itemCount.Should().Be(1);
    }

    [Fact]
    public async Task RestoringASnapshotResetsData()
    {
        var container = new ContainerMock();

        var item1 = new TestClass
        {
            Id = "Foo",
            PartitionKey = "Bar",
            MyValue = "FooBar"
        };
        await container.CreateItemAsync(item1, new PartitionKey("Bar"));

        var snapshot = container.CreateSnapshot();

        item1.MyValue = "NewValue";
        await container.UpsertItemAsync(item1, new PartitionKey("Bar"));

        container.RestoreSnapshot(snapshot);

        var dbItem = await container.ReadItemAsync<TestClass>("Foo", new PartitionKey("Bar"));
        dbItem.Resource.MyValue.Should().Be("FooBar");
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
