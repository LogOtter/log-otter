using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosPartitionTests(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos = testFixture.CreateTestCosmos();

    public async ValueTask InitializeAsync()
    {
        await _testCosmos.SetupAsync("/partitionKey");
    }

    public async ValueTask DisposeAsync()
    {
        await _testCosmos.CleanupAsync();
    }

    public void Dispose()
    {
        _testCosmos.Dispose();
    }

    [Fact]
    public async Task GivenDataInTwoPartitionsWhenReadingAPartitionDoesNotRetrieveTheOther()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                PartitionKey = "partition1",
            }
        );

        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD2",
                Name = "Bobetta Bobertson",
                PartitionKey = "partition2",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>("partition1", q => q);

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        realResults.Single().Name.ShouldBe("Bob Bobertson");
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenDataInTwoPartitionsWhenReadingAPartitionDoesNotCountTheOther()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                PartitionKey = "partition1",
            }
        );

        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD2",
                Name = "Bobetta Bobertson",
                PartitionKey = "partition2",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenCountingAQuery<TestModel>("partition1", q => q);

        realResults.ShouldBe(1);
        testResults.ShouldBe(1);
    }

    [Fact]
    public async Task GivenACrossPartitionQueryUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(null, q => q.Where(tm => tm.Name == "Bob Bobertson"));

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenACrossPartitionCountUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenCountingAQuery<TestModel>(null, q => q.Where(tm => tm.Name == "Bob Bobertson"));

        realResults.ShouldBe(1);
        testResults.ShouldBe(1);
    }
}
