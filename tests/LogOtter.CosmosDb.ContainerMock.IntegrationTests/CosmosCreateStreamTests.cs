using System.Text;
using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosCreateStreamTests : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos;

    public CosmosCreateStreamTests(IntegrationTestsFixture testFixture)
    {
        _testCosmos = testFixture.CreateTestCosmos();
    }

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
    public async Task CreateStreamNonExistingIsEquivalent()
    {
        var bytes = GetItemBytes(
            new TestModel
            {
                Id = "RECORD2",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1,
                PartitionKey = "test-cst1"
            }
        );

        await using var ms = new MemoryStream(bytes);
        var (real, test) = await _testCosmos.WhenCreatingStream(ms, new PartitionKey("test-cst1"));

        real.StatusCode.Should().Be(test.StatusCode);
    }

    [Fact]
    public async Task CreateStreamExistingIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2,
                PartitionKey = "test-cst2"
            }
        );

        var bytes = GetItemBytes(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1,
                PartitionKey = "test-cst2"
            }
        );

        await using var ms = new MemoryStream(bytes);
        var (real, test) = await _testCosmos.WhenCreatingStream(ms, new PartitionKey("test-cst2"));

        real.StatusCode.Should().Be(test.StatusCode);
    }

    [Fact]
    public async Task CreateStreamWithEmptyIdIsEquivalent()
    {
        var bytes = GetItemBytes(
            new TestModel
            {
                Id = string.Empty,
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1,
                PartitionKey = "test-cst2"
            }
        );

        await using var ms = new MemoryStream(bytes);
        var (real, test) = await _testCosmos.WhenCreatingStream(ms, new PartitionKey("test-cst2"));

        real.StatusCode.Should().Be(test.StatusCode);
    }

    [Fact]
    public async Task CreateStreamUniqueKeyViolationIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2,
                PartitionKey = "test-cst3"
            }
        );

        var bytes = GetItemBytes(
            new TestModel
            {
                Id = "RECORD2",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1,
                PartitionKey = "test-cst3"
            }
        );

        await using var ms = new MemoryStream(bytes);
        var (real, test) = await _testCosmos.WhenCreatingStream(ms, new PartitionKey("test-cst3"));

        real.StatusCode.Should().Be(test.StatusCode);
    }

    private static byte[] GetItemBytes(TestModel model)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
    }
}
