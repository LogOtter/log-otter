using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosUpsertTestsOnTripleUniqueKey(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos = testFixture.CreateTestCosmos();

    public async Task InitializeAsync()
    {
        var uniqueKeyPolicy = new UniqueKeyPolicy { UniqueKeys = { new UniqueKey { Paths = { "/CustomerId", "/ItemId", "/Type" } } } };

        await _testCosmos.SetupAsync("/partitionKey", uniqueKeyPolicy);
    }

    public async Task DisposeAsync()
    {
        await _testCosmos.CleanupAsync();
    }

    public void Dispose()
    {
        _testCosmos.Dispose();
    }

    [Fact]
    public async Task UpsertNonExistingIsEquivalent()
    {
        var (realResult, testResult) = await _testCosmos.WhenUpserting(
            new TripleUniqueKeyModel
            {
                Id = Guid.NewGuid(),
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1,
            }
        );

        realResult.StatusCode.ShouldBe(testResult.StatusCode);
        realResult.Resource.ShouldBeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task UpsertExistingIsEquivalent()
    {
        var id = Guid.NewGuid();

        await _testCosmos.GivenAnExistingItem(
            new TripleUniqueKeyModel
            {
                Id = id,
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1,
            }
        );

        var (realResult, testResult) = await _testCosmos.WhenUpserting(
            new TripleUniqueKeyModel
            {
                Id = id,
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1,
            }
        );

        realResult.StatusCode.ShouldBe(testResult.StatusCode);
        realResult.Resource.ShouldBeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task UpsertUniqueKeyViolationIsEquivalent()
    {
        var id = Guid.NewGuid();

        await _testCosmos.GivenAnExistingItem(
            new TripleUniqueKeyModel
            {
                Id = id,
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1,
            }
        );

        var (realException, testException) = await _testCosmos.WhenUpsertingProducesException(
            new TripleUniqueKeyModel
            {
                Id = Guid.NewGuid(),
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1,
            }
        );

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException!.StatusCode.ShouldBe(testException!.StatusCode);
        realException.ShouldBeOfType(testException.GetType());
    }

    [Fact]
    public async Task CreateNonExistingIsEquivalent()
    {
        var (realResult, testResult) = await _testCosmos.WhenCreating(
            new TripleUniqueKeyModel
            {
                Id = Guid.NewGuid(),
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1,
            }
        );

        realResult.StatusCode.ShouldBe(testResult.StatusCode);
        realResult.Resource.ShouldBeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task CreateUniqueKeyViolationIsEquivalent()
    {
        var id = Guid.NewGuid();

        await _testCosmos.GivenAnExistingItem(
            new TripleUniqueKeyModel
            {
                Id = id,
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1,
            }
        );

        var (realException, testException) = await _testCosmos.WhenCreatingProducesException(
            new TripleUniqueKeyModel
            {
                Id = Guid.NewGuid(),
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1,
            }
        );

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException!.StatusCode.ShouldBe(testException!.StatusCode);
        realException.ShouldBeOfType(testException.GetType());
    }
}
