using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace CosmosTestHelpers.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosReadItemStreamTests : IAsyncLifetime, IDisposable
{
    private TestCosmos _testCosmos;

    [Fact]
    public async Task ReadWithEmptyIdIsEquivalent()
    {
        var (realResponse, testResponse) = await _testCosmos.WhenReadItemStream(string.Empty);

        realResponse.Should().NotBeNull();
        testResponse.Should().NotBeNull();
        realResponse.StatusCode.Should().Be(testResponse.StatusCode);
    } 
        
    [Fact]
    public async Task ReadWithNullIdIsEquivalent()
    {
        var (realException, testException) = await _testCosmos.WhenReadItemStreamProducesException(null);

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException.Should().BeOfType(testException.GetType());
            
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