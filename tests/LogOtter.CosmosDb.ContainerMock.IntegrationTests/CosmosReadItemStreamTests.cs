using Microsoft.Azure.Cosmos;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosReadItemStreamTests(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
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
    public async Task ReadWithEmptyIdIsEquivalent()
    {
        var (realResponse, testResponse) = await _testCosmos.WhenReadItemStream(string.Empty);

        realResponse.ShouldNotBeNull();
        testResponse.ShouldNotBeNull();
        realResponse.StatusCode.ShouldBe(testResponse.StatusCode);
    }

    [Fact]
    public async Task ReadWithNullIdIsEquivalent()
    {
        var (realException, testException) = await _testCosmos.WhenReadItemStreamProducesException(null);

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException.ShouldBeOfType(testException!.GetType());

        if (realException is CosmosException realCosmosException && testException is CosmosException testCosmosException)
        {
            realCosmosException.StatusCode.ShouldBe(testCosmosException.StatusCode);
        }
    }
}
