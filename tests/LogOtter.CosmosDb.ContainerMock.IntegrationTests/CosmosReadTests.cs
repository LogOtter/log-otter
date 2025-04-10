using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosReadTests(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
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
        var (realException, testException) = await _testCosmos.WhenReadItemProducesException<TestModel>(string.Empty);

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException.ShouldBeOfType(testException!.GetType());

        if (realException is CosmosException realCosmosException && testException is CosmosException testCosmosException)
        {
            realCosmosException.StatusCode.ShouldBe(testCosmosException.StatusCode);
        }
    }

    [Fact]
    public async Task ReadWithInvalidIdIsEquivalent()
    {
        var isUsingCosmosDbEmulator = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_COSMOS_CONNECTION_STRING"));
        Assert.SkipWhen(isUsingCosmosDbEmulator, "The CosmosDb emulator does not behave like the real CosmosDb in this scenario");

        var (realException, testException) = await _testCosmos.WhenReadItemProducesException<TestModel>("#");

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException.ShouldBeOfType(testException!.GetType());

        if (realException is CosmosException realCosmosException && testException is CosmosException testCosmosException)
        {
            realCosmosException.StatusCode.ShouldBe(testCosmosException.StatusCode);
        }
    }

    [Fact]
    public async Task ReadWithNullIdIsEquivalent()
    {
        var (realException, testException) = await _testCosmos.WhenReadItemProducesException<TestModel>(null);

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException.ShouldBeOfType(testException!.GetType());

        if (realException is CosmosException realCosmosException && testException is CosmosException testCosmosException)
        {
            realCosmosException.StatusCode.ShouldBe(testCosmosException.StatusCode);
        }
    }
}
