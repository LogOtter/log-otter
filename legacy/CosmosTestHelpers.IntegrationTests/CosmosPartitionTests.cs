using CosmosTestHelpers.IntegrationTests.TestModels;
using FluentAssertions;
using Xunit;

namespace CosmosTestHelpers.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosPartitionTests : IAsyncLifetime, IDisposable
{
    private TestCosmos _testCosmos;

    [Fact]
    public async Task GivenDataInTwoPartitionsWhenReadingAPartitionDoesNotRetrieveTheOther()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            PartitionKey = "partition1"
        });

        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD2",
            Name = "Bobetta Bobertson",
            PartitionKey = "partition2"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>("partition1", q => q);

        realResults.Count.Should().Be(1);
        realResults.Single().Name.Should().Be("Bob Bobertson");
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenDataInTwoPartitionsWhenReadingAPartitionDoesNotCountTheOther()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            PartitionKey = "partition1"
        });

        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD2",
            Name = "Bobetta Bobertson",
            PartitionKey = "partition2"
        });

        var (realResults, testResults) = await _testCosmos.WhenCountingAQuery<TestModel>("partition1", q => q);

        realResults.Should().Be(1);
        testResults.Should().Be(1);
    }

    [Fact]
    public async Task GivenACrossPartitionQueryUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            null,
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        realResults.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenACrossPartitionCountUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) =
            await _testCosmos.WhenCountingAQuery<TestModel>(
                null,
                q => q.Where(tm => tm.Name == "Bob Bobertson")
            );

        realResults.Should().Be(1);
        testResults.Should().Be(1);
    }

    public Task InitializeAsync()
    {
        _testCosmos = new TestCosmos();
        return _testCosmos.SetupAsync("/partitionKey");
    }

    public async Task DisposeAsync()
    {
        await _testCosmos.CleanupAsync();
    }

    public void Dispose()
    {
        _testCosmos?.Dispose();
    }
}