using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosReplaceTests : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos;

    public CosmosReplaceTests(IntegrationTestsFixture testFixture)
    {
        _testCosmos = testFixture.CreateTestCosmos();
    }

    [Fact]
    public async Task ReplaceExistingWithWrongETagIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value2
        });

        var (realException, testException) = await _testCosmos.WhenReplacingProducesException(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1
            },
            "RECORD1",
            new ItemRequestOptions
            {
                IfMatchEtag = Guid.NewGuid().ToString()
            },
            new ItemRequestOptions
            {
                IfMatchEtag = Guid.NewGuid().ToString()
            });

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException!.StatusCode.Should().Be(testException!.StatusCode);
        realException.Should().BeOfType(testException.GetType());
    }

    [Fact]
    public async Task ReplaceExistingWithCorrectETagIsEquivalent()
    {
        var (testETag, realETag) = await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value2
        });

        var (realResult, testResult) = await _testCosmos.WhenReplacing(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1
            },
            "RECORD1",
            new ItemRequestOptions
            {
                IfMatchEtag = testETag
            },
            new ItemRequestOptions
            {
                IfMatchEtag = realETag
            });

        realResult.StatusCode.Should().Be(testResult.StatusCode);
        realResult.Resource.Should().BeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task ReplaceNonExistentItemThrowsTheSameException()
    {
        var (realException, testException) = await _testCosmos.WhenReplacingProducesException(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1
            },
            "RECORD1"
        );

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException!.StatusCode.Should().Be(testException!.StatusCode);
        realException.Should().BeOfType(testException.GetType());
    }

    public async Task InitializeAsync()
    {
        var uniqueKeyPolicy = new UniqueKeyPolicy
        {
            UniqueKeys = { new UniqueKey { Paths = { "/Name" } } }
        };
        
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
}