using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos.Linq;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosQueryEquivalencyTests(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos = testFixture.CreateTestCosmos();

    public Task InitializeAsync()
    {
        return _testCosmos.SetupAsync("/partitionKey");
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
    public async Task GivenAQueryUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task IsNullWorks()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = null,
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>("partition", q => q.Where(tm => tm.Name.IsNull()));

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task IsDefinedWorksOnNull()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = null,
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>("partition", q => q.Where(tm => tm.Name.IsDefined()));

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenACountUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD2",
                Name = "Bobetta Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenCountingAQuery<TestModel>("partition", q => q.Where(tm => tm.Name == "Bob Bobertson"));

        realResults.ShouldBe(1);
        testResults.ShouldBe(1);
    }

    [Fact]
    public async Task GivenAQueryForAllItemsInAPartitionUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>("partition");

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingContainsOnAnEnumWithAToStringWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => new[] { TestEnum.Value2.ToString() }.Contains(tm.EnumValue.ToString()))
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAValueFromAMethodWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = true,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>("partition", q => q.Where(tm => tm.Value == GetTrue()));

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAValueFromAMethodWithArgsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = true,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>("partition", q => q.Where(tm => tm.Value == GetBool(true)));

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAValueFromAMethodAgainstModelWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = true,
                PartitionKey = "partition",
            }
        );

        Func<Task> action = () => _testCosmos.WhenExecutingAQuery<TestModel>("partition", q => q.Where(tm => tm.GetBoolValue() == true));

        var exceptionAssertions = await action.ShouldThrowAsync<CosmosEquivalencyException>();
        exceptionAssertions.RealException.ShouldNotBeNull();
        exceptionAssertions.TestException.ShouldNotBeNull();
    }

    [Fact]
    public async Task GivenAQueryUsingProjectionWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Select(tm => new TestModel { Name = "Projected" })
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNotEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name != "Bobbeta Bobertson")
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNotXorWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>("partition", q => q.Where(tm => !(tm.Value ^ false)));

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(0);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingXorWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>("partition", q => q.Where(tm => tm.Value ^ true));

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(0);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingToUpperWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name != null && tm.Name.ToUpper() == "BOB BOBERTSON")
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingToLowerWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name != null && tm.Name.ToLower() == "bob bobertson")
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAnyInASubQueryWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Children = new[] { new SubModel { Value = "bob bobertson" } },
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Children != null && tm.Children.Any(c => c.Value == "bob bobertson"))
        );

        realResults.ShouldNotBeNull();
        realResults!.Count.ShouldBe(1);
        testResults.ShouldBeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingToUpperInvariantWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        Func<Task> action = () =>
            _testCosmos.WhenExecutingAQuery<TestModel>(
                "partition",
                q => q.Where(tm => tm.Name != null && tm.Name.ToUpperInvariant() == "BOB BOBERTSON")
            );

        var exceptionAssertions = await action.ShouldThrowAsync<CosmosEquivalencyException>();
        exceptionAssertions.RealException.ShouldNotBeNull();
        exceptionAssertions.TestException.ShouldNotBeNull();
    }

    [Fact]
    public async Task GivenAQueryUsingToLowerInvariantWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        Func<Task> action = () =>
            _testCosmos.WhenExecutingAQuery<TestModel>(
                "partition",
                q => q.Where(tm => tm.Name != null && tm.Name.ToLowerInvariant() == "bob bobertson")
            );

        var exceptionAssertions = await action.ShouldThrowAsync<CosmosEquivalencyException>();
        exceptionAssertions.RealException.ShouldNotBeNull();
        exceptionAssertions.TestException.ShouldNotBeNull();
    }

    [Fact]
    public async Task GivenAQueryUsingToAnyWhenExecutingThenBothShouldWork()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name == "bob bobertson")
        );

        realResults.ShouldNotBeNull();
        testResults.ShouldNotBeNull();

        testResults!.Any().ShouldBe(realResults!.Any());
    }

    [Fact]
    public async Task GivenAQueryUsingFirstOrDefaultWhenExecutingThenBothShouldWork()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        realResults.ShouldNotBeNull();
        testResults.ShouldNotBeNull();

        realResults!.FirstOrDefault().ShouldNotBeNull();
        testResults!.FirstOrDefault().ShouldBeEquivalentTo(realResults!.FirstOrDefault());
    }

    [Fact]
    public async Task GivenAQueryUsingSingleOrDefaultWhenExecutingThenBothShouldWork()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        realResults.ShouldNotBeNull();
        testResults.ShouldNotBeNull();

        realResults!.SingleOrDefault().ShouldNotBeNull();
        testResults!.SingleOrDefault().ShouldBeEquivalentTo(realResults!.SingleOrDefault());
    }

    [Fact]
    public async Task GivenAQueryUsingStringEqualsMethodThenBothShouldWork()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name != null && tm.Name.Equals("Bob Bobertson"))
        );

        realResults.ShouldNotBeNull();
        testResults.ShouldNotBeNull();

        realResults!.SingleOrDefault().ShouldNotBeNull();
        testResults!.SingleOrDefault().ShouldBeEquivalentTo(realResults!.SingleOrDefault());
    }

    [Theory]
    [InlineData(StringComparison.InvariantCultureIgnoreCase)]
    [InlineData(StringComparison.OrdinalIgnoreCase)]
    public async Task GivenAQueryUsingStringEqualsMethodWithCultureDifferentCaseThenBothShouldWork(StringComparison comparisonType)
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name != null && tm.Name.Equals("bob bobertson", comparisonType))
        );

        realResults.ShouldNotBeNull();
        testResults.ShouldNotBeNull();

        realResults!.SingleOrDefault().ShouldNotBeNull();
        testResults!.SingleOrDefault().ShouldBeEquivalentTo(realResults!.SingleOrDefault());
    }

    [Theory]
    [InlineData(StringComparison.InvariantCulture)]
    [InlineData(StringComparison.Ordinal)]
    public async Task GivenAQueryUsingStringEqualsMethodWithCultureSameCaseThenBothShouldWork(StringComparison comparisonType)
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "partition",
            }
        );

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name != null && tm.Name.Equals("Bob Bobertson", comparisonType))
        );

        realResults.ShouldNotBeNull();
        testResults.ShouldNotBeNull();

        realResults!.SingleOrDefault().ShouldNotBeNull();
        testResults!.SingleOrDefault().ShouldBeEquivalentTo(realResults!.SingleOrDefault());
    }

    private bool GetTrue()
    {
        return true;
    }

    private bool GetBool(bool b)
    {
        return b;
    }
}
