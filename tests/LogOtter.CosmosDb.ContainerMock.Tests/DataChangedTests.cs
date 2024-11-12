using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.ContainerMockData;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests;

public class DataChangedTests
{
    [Fact]
    public async Task UpsertingNewItemToContainerInvokesChangedEvent()
    {
        var sut = new ContainerMock();
        bool dataChangeCalled = false;
        sut.DataChanged += (sender, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Created);
        };
        await sut.UpsertItemAsync<TestClass>(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value1"
            },
            new PartitionKey("APartition")
        );

        dataChangeCalled.Should().Be(true);
    }

    [Fact]
    public async Task UpsertingExistingItemToContainerInvokesChangedEvent()
    {
        var sut = new ContainerMock();
        await sut.UpsertItemAsync<TestClass>(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value1"
            },
            new PartitionKey("APartition")
        );

        bool dataChangeCalled = false;
        sut.DataChanged += (sender, args) =>
        {
            dataChangeCalled = true;
            args.Should().NotBeNull();
            args.Operation.Should().Be(Operation.Updated);
        };
        await sut.UpsertItemAsync<TestClass>(
            new TestClass()
            {
                Id = "MyId",
                PartitionKey = "APartition",
                MyValue = "Value2"
            },
            new PartitionKey("APartition")
        );

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
