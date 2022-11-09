using CosmosTestHelpers.IntegrationTests.TestModels;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace CosmosTestHelpers.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosUpsertTestsOnTripleUniqueKey : IAsyncLifetime, IDisposable
{
    private TestCosmos _testCosmos;

    [Fact]
    public async Task UpsertNonExistingIsEquivalent()
    {
        var (realResult, testResult) = await _testCosmos.WhenUpserting(
            new TripleUniqueKeyModel
            {
                Id = Guid.NewGuid(),
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1
            });

        realResult.StatusCode.Should().Be(testResult.StatusCode);
        realResult.Resource.Should().BeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task UpsertExistingIsEquivalent()
    {
        var id = Guid.NewGuid();

        await _testCosmos.GivenAnExistingItem(new TripleUniqueKeyModel
        {
            Id = id,
            CustomerId = "Fred Blogs",
            ItemId = "MT12342",
            Type = TestEnum.Value1
        });

        var (realResult, testResult) = await _testCosmos.WhenUpserting(
            new TripleUniqueKeyModel
            {
                Id = id,
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1
            });

        realResult.StatusCode.Should().Be(testResult.StatusCode);
        realResult.Resource.Should().BeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task UpsertUniqueKeyViolationIsEquivalent()
    {
        var id = Guid.NewGuid();

        await _testCosmos.GivenAnExistingItem(new TripleUniqueKeyModel
        {
            Id = id,
            CustomerId = "Fred Blogs",
            ItemId = "MT12342",
            Type = TestEnum.Value1
        });

        var (realException, testException) = await _testCosmos.WhenUpsertingProducesException(
            new TripleUniqueKeyModel
            {
                Id = Guid.NewGuid(),
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1
            });

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException.StatusCode.Should().Be(testException.StatusCode);
        realException.Should().BeOfType(testException.GetType());
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
                Type = TestEnum.Value1
            });

        realResult.StatusCode.Should().Be(testResult.StatusCode);
        realResult.Resource.Should().BeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task CreateUniqueKeyViolationIsEquivalent()
    {
        var id = Guid.NewGuid();

        await _testCosmos.GivenAnExistingItem(new TripleUniqueKeyModel
        {
            Id = id,
            CustomerId = "Fred Blogs",
            ItemId = "MT12342",
            Type = TestEnum.Value1
        });

        var (realException, testException) = await _testCosmos.WhenCreatingProducesException(
            new TripleUniqueKeyModel
            {
                Id = Guid.NewGuid(),
                CustomerId = "Fred Blogs",
                ItemId = "MT12342",
                Type = TestEnum.Value1
            });

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException.StatusCode.Should().Be(testException.StatusCode);
        realException.Should().BeOfType(testException.GetType());
    }

    public Task InitializeAsync()
    {
        var uniqueKeyPolicy = new UniqueKeyPolicy
        {
            UniqueKeys = { new UniqueKey { Paths = { "/CustomerId", "/ItemId", "/Type" } } }
        };

        _testCosmos = new TestCosmos();
        return _testCosmos.SetupAsync("/partitionKey", uniqueKeyPolicy);
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