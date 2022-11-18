using System.Text;
using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosUpsertStreamTests : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos;

    public CosmosUpsertStreamTests(IntegrationTestsFixture testFixture)
    {
        _testCosmos = testFixture.CreateTestCosmos();
    }

    [Fact]
    public async Task UpsertStreamNonExistingIsEquivalent()
    {
        var bytes = GetItemBytes(new TestModel
        {
            Id = "RECORD2",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value1,
            PartitionKey = "test"
        });

        await using var ms = new MemoryStream(bytes);
        var (real, test) = await _testCosmos.WhenUpsertingStream(ms, new PartitionKey("test"));

        real.StatusCode.Should().Be(test.StatusCode);
    }

    [Fact]
    public async Task UpsertStreamExistingIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value2,
            PartitionKey = "test"
        });

        var bytes = GetItemBytes(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value1,
            PartitionKey = "test"
        });

        await using var ms = new MemoryStream(bytes);
        var (real, test) = await _testCosmos.WhenUpsertingStream(ms, new PartitionKey("test"));

        real.StatusCode.Should().Be(test.StatusCode);
    }

    [Fact]
    public async Task UpsertStreamExistingWithCorrectETagIsEquivalent()
    {
        var (testETag, realETag) = await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value2,
            PartitionKey = "test"
        });

        var bytes = GetItemBytes(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value1,
            PartitionKey = "test"
        });

        await using var ms = new MemoryStream(bytes);
        var (real, test) = await _testCosmos.WhenUpsertingStream(
            ms,
            new PartitionKey("test"),
            new ItemRequestOptions
            {
                IfMatchEtag = testETag
            },
            new ItemRequestOptions
            {
                IfMatchEtag = realETag
            });

        real.StatusCode.Should().Be(test.StatusCode);
    }

    [Fact]
    public async Task UpsertStreamExistingWithWrongETagIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value2,
            PartitionKey = "test"
        });

        var bytes = GetItemBytes(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value1,
            PartitionKey = "test"
        });

        await using var ms = new MemoryStream(bytes);
        var (real, test) = await _testCosmos.WhenUpsertingStream(
            ms,
            new PartitionKey("test"),
            new ItemRequestOptions
            {
                IfMatchEtag = Guid.NewGuid().ToString()
            },
            new ItemRequestOptions
            {
                IfMatchEtag = Guid.NewGuid().ToString()
            });

        real.StatusCode.Should().Be(test.StatusCode);
    }

    [Fact]
    public async Task UpsertStreamUniqueKeyViolationIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value2,
            PartitionKey = "test"
        });

        var bytes = GetItemBytes(new TestModel
        {
            Id = "RECORD2",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value1,
            PartitionKey = "test"
        });

        await using var ms = new MemoryStream(bytes);
        var (real, test) = await _testCosmos.WhenUpsertingStream(ms, new PartitionKey("test"));

        real.StatusCode.Should().Be(test.StatusCode);
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

    private static byte[] GetItemBytes(TestModel model)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
    }
}