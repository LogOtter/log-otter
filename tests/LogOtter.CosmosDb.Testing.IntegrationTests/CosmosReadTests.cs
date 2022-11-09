using FluentAssertions;
using LogOtter.CosmosDb.Testing.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace LogOtter.CosmosDb.Testing.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosReadTests : IAsyncLifetime, IDisposable
{
    private TestCosmos _testCosmos = default!;

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
        
    [Fact]
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