using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosReadTests : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos;

    public CosmosReadTests(IntegrationTestsFixture testFixture)
    {
        _testCosmos = testFixture.CreateTestCosmos();
    }

    [Fact]
    public async Task ReadWithEmptyIdIsEquivalent()
    {
        var (realException, testException) = await _testCosmos.WhenReadItemProducesException<TestModel>(string.Empty);

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException.Should().BeOfType(testException!.GetType());

        if (realException is CosmosException realCosmosException && testException is CosmosException testCosmosException)
        {
            realCosmosException.StatusCode.Should().Be(testCosmosException.StatusCode);
        }
    } 
        
    //TODO: Verify if this is just a problem with the emulator
    [Fact(Skip = "Failing with Cosmos Emulator")]
    public async Task ReadWithInvalidIdIsEquivalent()
    {
        var (realException, testException) = await _testCosmos.WhenReadItemProducesException<TestModel>("#");

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException.Should().BeOfType(testException!.GetType());

        if (realException is CosmosException realCosmosException && testException is CosmosException testCosmosException)
        {
            realCosmosException.StatusCode.Should().Be(testCosmosException.StatusCode);
        }
    } 
        
    [Fact]
    public async Task ReadWithNullIdIsEquivalent()
    {
        var (realException, testException) = await _testCosmos.WhenReadItemProducesException<TestModel>(null);

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException.Should().BeOfType(testException!.GetType());
            
        if (realException is CosmosException realCosmosException && testException is CosmosException testCosmosException)
        {
            realCosmosException.StatusCode.Should().Be(testCosmosException.StatusCode);
        }
    } 
        
    public async Task InitializeAsync()
    {
        await _testCosmos.SetupAsync("/partitionKey");
    }

    public async Task DisposeAsync()
    {
        await _testCosmos.CleanupAsync();
    }

    public void Dispose()
    {
        _testCosmos.Dispose();
    }
}