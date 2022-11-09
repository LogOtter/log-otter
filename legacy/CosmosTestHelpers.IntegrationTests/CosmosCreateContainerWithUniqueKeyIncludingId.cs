using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace CosmosTestHelpers.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosCreateContainerWithUniqueKeyIncludingId : IAsyncLifetime, IDisposable
{
    private TestCosmos _testCosmos;

    [Fact]
    public async Task CreateUniqueKeyViolationIsEquivalent()
    {
        var uniqueKeyPolicy = new UniqueKeyPolicy
        {
            UniqueKeys = { new UniqueKey { Paths = { "/id", "/ItemId", "/Type" } } }
        };
        
        var (realException, testException) = await _testCosmos.SetupAsyncProducesExceptions(
            "/partitionKey", 
            uniqueKeyPolicy
        );

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException.StatusCode.Should().Be(testException.StatusCode);
        realException.Should().BeOfType(testException.GetType());
    }

    public Task InitializeAsync()
    {
        _testCosmos = new TestCosmos();
        return Task.CompletedTask;
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