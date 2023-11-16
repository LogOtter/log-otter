﻿using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosUpsertTests(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
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
    public async Task UpsertNonExistingIsEquivalent()
    {
        var (realResult, testResult) = await _testCosmos.WhenUpserting(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Fred Blogs",
                EnumValue = TestEnum.Value1
            }
        );

        realResult.StatusCode.Should().Be(testResult.StatusCode);
        realResult.Resource.Should().BeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task UpsertExistingIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2
            }
        );

        var (realResult, testResult) = await _testCosmos.WhenUpserting(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1
            }
        );

        realResult.StatusCode.Should().Be(testResult.StatusCode);
        realResult.Resource.Should().BeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task UpsertExistingWithWrongETagIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2
            }
        );

        var (realException, testException) = await _testCosmos.WhenUpsertingProducesException(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1
            },
            new ItemRequestOptions { IfMatchEtag = Guid.NewGuid().ToString() },
            new ItemRequestOptions { IfMatchEtag = Guid.NewGuid().ToString() }
        );

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException!.StatusCode.Should().Be(testException!.StatusCode);
        realException.Should().BeOfType(testException.GetType());
    }

    [Fact]
    public async Task UpsertExistingWithCorrectETagIsEquivalent()
    {
        var (testETag, realETag) = await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2
            }
        );

        var (realResult, testResult) = await _testCosmos.WhenUpserting(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1
            },
            new ItemRequestOptions { IfMatchEtag = testETag },
            new ItemRequestOptions { IfMatchEtag = realETag }
        );

        realResult.StatusCode.Should().Be(testResult.StatusCode);
        realResult.Resource.Should().BeEquivalentTo(testResult.Resource);
    }

    [Fact]
    public async Task UpsertUniqueKeyViolationIsEquivalent()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2
            }
        );

        var (realException, testException) = await _testCosmos.WhenUpsertingProducesException(
            new TestModel
            {
                Id = "RECORD2",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value1
            }
        );

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException!.StatusCode.Should().Be(testException!.StatusCode);
        realException.Should().BeOfType(testException.GetType());
    }
}
