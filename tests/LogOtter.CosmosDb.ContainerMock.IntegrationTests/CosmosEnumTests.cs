﻿using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Shouldly;
using Xunit;

// Deliberately testing this
#pragma warning disable 472

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosEnumTests(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos = testFixture.CreateTestCosmos();

    public async ValueTask InitializeAsync()
    {
        await _testCosmos.SetupAsync("/partitionKey");
    }

    public async ValueTask DisposeAsync()
    {
        await _testCosmos.CleanupAsync();
    }

    public void Dispose()
    {
        _testCosmos.Dispose();
    }

    [Fact]
    public async Task GivenAQueryUsingAValueForNullableEnumWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition-et1",
                NullableEnum = TestEnum.Value1,
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition-et1",
            q => q.Where(tm => tm.NullableEnum!.Value != null && tm.NullableEnum == TestEnum.Value1)
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAValueForNullableOnSubModelEnumWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                PartitionKey = "partition-et2",
                OnlyChild = new SubModel { NullableEnum = TestEnum.Value1 },
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition-et2",
            q => q.Where(tm => tm.OnlyChild != null && tm.OnlyChild.NullableEnum!.Value != null && tm.OnlyChild.NullableEnum == TestEnum.Value1)
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAValueForNullableEnumWhenExecutingThenTheResultsShouldMatchReversedEquality()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition-et3",
                NullableEnum = TestEnum.Value1,
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition-et3",
            q => q.Where(tm => null != tm.NullableEnum!.Value && TestEnum.Value1 == tm.NullableEnum)
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNullableEnumWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition-et4",
                NullableEnum = null,
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition-et4",
            q => q.Where(tm => tm.NullableEnum!.Value == null)
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNullableEnumOnSubmodelWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition-et5",
                OnlyChild = new SubModel { NullableEnum = null },
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition-et5",
            q => q.Where(tm => tm.OnlyChild!.NullableEnum!.Value == null)
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNotNullEnumOnSubmodelWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition-et6",
                OnlyChild = new SubModel { NullableEnum = null },
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition-et6",
            q => q.Where(tm => tm.OnlyChild!.NullableEnum!.Value != null)
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(0);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNullableEnumWhenExecutingThenTheResultsShouldMatchReversedEquality()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition-et7",
                NullableEnum = null,
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition-et7",
            q => q.Where(tm => null == tm.NullableEnum!.Value)
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAValueForNullableNonStringEnumWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD2",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition-et8",
                NullableEnumNotString = TestEnum.Value1,
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition-et8",
            q => q.Where(tm => tm.NullableEnumNotString!.Value != null && tm.NullableEnumNotString == TestEnum.Value1)
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNullableNonStringEnumWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition-et9",
                NullableEnumNotString = null,
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition-et9",
            q => q.Where(tm => tm.NullableEnumNotString!.Value == null)
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }
}
