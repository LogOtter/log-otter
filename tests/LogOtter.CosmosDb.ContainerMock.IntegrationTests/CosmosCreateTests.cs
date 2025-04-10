using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosCreateTests(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos = testFixture.CreateTestCosmos();

    public async ValueTask InitializeAsync()
    {
        var uniqueKeyPolicy = new UniqueKeyPolicy { UniqueKeys = { new UniqueKey { Paths = { "/Name" } } } };

        await _testCosmos.SetupAsync("/partitionKey", uniqueKeyPolicy);
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
    public async Task CreateNonExistingIsEquivalent()
    {
        var testModel = new TestModel
        {
            Id = "RECORD1",
            Name = "Fred Blogs",
            EnumValue = TestEnum.Value1,
        };

        var (realResult, testResult) = await _testCosmos.WhenCreating(testModel, new PartitionKey(testModel.PartitionKey));

        realResult.StatusCode.ShouldBe(testResult.StatusCode);
        realResult.Resource.ShouldBeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task CreateEmptyIdIsEquivalent()
    {
        var testModel = new TestModel
        {
            Id = string.Empty,
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value1,
        };

        var (realException, testException) = await _testCosmos.WhenCreatingProducesException(testModel, new PartitionKey(testModel.PartitionKey));

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException!.StatusCode.ShouldBe(testException!.StatusCode);
        realException.ShouldBeOfType(testException.GetType());
    }

    [Fact]
    public async Task CreateExistingIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2,
            }
        );

        var testModel = new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value1,
        };

        var (realException, testException) = await _testCosmos.WhenCreatingProducesException(testModel, new PartitionKey(testModel.PartitionKey));

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException!.StatusCode.ShouldBe(testException!.StatusCode);
        realException.ShouldBeOfType(testException.GetType());
    }

    [Fact]
    public async Task CreateWithMismatchedPartitionIsEquivalent()
    {
        var testModel = new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value1,
        };

        var (realException, testException) = await _testCosmos.WhenCreatingProducesException(testModel, PartitionKey.None);

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException!.StatusCode.ShouldBe(testException!.StatusCode);
        realException.ShouldBeOfType(testException.GetType());
    }
}
