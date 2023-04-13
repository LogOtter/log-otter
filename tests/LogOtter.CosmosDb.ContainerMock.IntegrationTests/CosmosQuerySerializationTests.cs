using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosQuerySerializationTests
{
    private readonly IntegrationTestsFixture _fixture;

    public CosmosQuerySerializationTests(IntegrationTestsFixture testFixture)
    {
        _fixture = testFixture;
    }

    [Fact]
    public async Task GivenACustomSerializer_WhenQueryingWithLingSerializationOptions_ShouldRetrieveTheExpectedRecords()
    {
        var cosmosClientOptions = new CosmosClientOptions
        {
            Serializer = new
        };
        var testCosmos = _fixture.CreateTestCosmos();
        await testCosmos.SetupAsync("/partitionKey");
        await testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition"
            }
        );

        var (realResults, testResults) = await testCosmos.WhenExecutingAQuery<TestModel>("partition", q => q.Where(tm => tm.Name == "Bob Bobertson"));

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }
}
