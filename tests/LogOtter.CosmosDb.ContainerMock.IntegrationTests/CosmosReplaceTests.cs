using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosReplaceTests(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos = testFixture.CreateTestCosmos();

    public async Task InitializeAsync()
    {
        var uniqueKeyPolicy = new UniqueKeyPolicy { UniqueKeys = { new UniqueKey { Paths = { "/Name" } } } };

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
    public async Task ReplaceExistingWithWrongETagIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2,
            }
        );

        var (realException, testException) = await _testCosmos.WhenReplacingProducesException(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1,
            },
            "RECORD1",
            new ItemRequestOptions { IfMatchEtag = Guid.NewGuid().ToString() },
            new ItemRequestOptions { IfMatchEtag = Guid.NewGuid().ToString() }
        );

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException!.StatusCode.ShouldBe(testException!.StatusCode);
        realException.ShouldBeOfType(testException.GetType());
    }

    [Fact]
    public async Task ReplaceExistingWithCorrectETagIsEquivalent()
    {
        var (testETag, realETag) = await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2,
            }
        );

        var (realResult, testResult) = await _testCosmos.WhenReplacing(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1,
            },
            "RECORD1",
            new ItemRequestOptions { IfMatchEtag = testETag },
            new ItemRequestOptions { IfMatchEtag = realETag }
        );

        realResult.StatusCode.ShouldBe(testResult.StatusCode);
        realResult.Resource.ShouldBeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task ReplaceNonExistentItemThrowsTheSameException()
    {
        var (realException, testException) = await _testCosmos.WhenReplacingProducesException(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1,
            },
            "RECORD1"
        );

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
        realException!.StatusCode.ShouldBe(testException!.StatusCode);
        realException.ShouldBeOfType(testException.GetType());
    }
}
