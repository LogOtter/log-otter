﻿using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosReadTests(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos = testFixture.CreateTestCosmos();

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

    [SkippableFact]
    public async Task ReadWithInvalidIdIsEquivalent()
    {
        var isUsingCosmosDbEmulator = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_COSMOS_CONNECTION_STRING"));
        Skip.If(isUsingCosmosDbEmulator, "The CosmosDb emulator does not behave like the real CosmosDb in this scenario");

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
}
