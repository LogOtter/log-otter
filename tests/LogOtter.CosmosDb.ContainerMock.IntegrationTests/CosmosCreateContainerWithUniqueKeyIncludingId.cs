using Microsoft.Azure.Cosmos;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosCreateContainerWithUniqueKeyIncludingId(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos = testFixture.CreateTestCosmos();

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
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
    public async Task CreateUniqueKeyViolationIsEquivalent()
    {
        var uniqueKeyPolicy = new UniqueKeyPolicy { UniqueKeys = { new UniqueKey { Paths = { "/id", "/ItemId", "/Type" } } } };

        var (realException, testException) = await _testCosmos.SetupAsyncProducesExceptions("/partitionKey", uniqueKeyPolicy);

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException!.StatusCode.ShouldBe(testException!.StatusCode);
        realException.ShouldBeOfType(testException.GetType());
    }
}
