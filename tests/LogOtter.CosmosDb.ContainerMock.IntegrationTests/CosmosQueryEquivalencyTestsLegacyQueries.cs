using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Obsolete("Kept to test legacy querying until the CreateItemLinqQueryable method is removed")]
[Collection("Integration Tests")]
public sealed class CosmosQueryEquivalencyTestsLegacyQueries(IntegrationTestsFixture testFixture) : IAsyncLifetime, IDisposable
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
    public async Task GivenAQueryUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Where(tm => tm.Name == "Bob Bobertson"));

        realResults.Count().ShouldBe(1);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q =>
            q.Where(tm => new[] { TestEnum.Value2.ToString() }.Contains(tm.EnumValue.ToString()))
        );

        realResults.Count().ShouldBe(1);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Where(tm => tm.Value == GetTrue()));

        realResults.Count().ShouldBe(1);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
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

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Where(tm => tm.GetBoolValue() == true));

        var realResultsAction = new Func<TestModel?>(() => realResults.SingleOrDefault());
        var testResultsAction = new Func<TestModel?>(() => testResults.SingleOrDefault());

        realResultsAction.ShouldThrow<Exception>();
        testResultsAction.ShouldThrow<Exception>();
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Where(tm => tm.Value == GetBool(true)));

        realResults.Count().ShouldBe(1);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Select(tm => new TestModel { Name = "Projected" }));

        realResults.Count().ShouldBe(1);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Where(tm => tm.Name != "Bobbeta Bobertson"));

        realResults.Count().ShouldBe(1);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Where(tm => !(tm.Value ^ false)));

        realResults.Count().ShouldBe(0);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Where(tm => tm.Value ^ true));

        realResults.Count().ShouldBe(0);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q =>
            q.Where(tm => tm.Name != null && tm.Name.ToUpper() == "BOB BOBERTSON")
        );

        realResults.Count().ShouldBe(1);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q =>
            q.Where(tm => tm.Name != null && tm.Name.ToLower() == "bob bobertson")
        );

        realResults.Count().ShouldBe(1);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
    }

    [Fact]
    public async Task GivenAQueryUsingAnyInASubQueryWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel { Id = "RECORD1", Children = new[] { new SubModel { Value = "bob bobertson" } } });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q =>
            q.Where(tm => tm.Children != null && tm.Children.Any(c => c.Value == "bob bobertson"))
        );

        realResults.Count().ShouldBe(1);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q =>
            q.Where(tm => tm.Name != null && tm.Name.ToUpperInvariant() == "BOB BOBERTSON")
        );

        var realResultsAction = new Func<IList<TestModel>>(() => realResults.ToList());
        var testResultsAction = new Func<IList<TestModel>>(() => testResults.ToList());

        realResultsAction.ShouldThrow<Exception>();
        testResultsAction.ShouldThrow<Exception>();
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
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q =>
            q.Where(tm => tm.Name != null && tm.Name.ToLowerInvariant() == "bob bobertson")
        );

        var realResultsAction = new Func<IList<TestModel>>(() => realResults.ToList());
        var testResultsAction = new Func<IList<TestModel>>(() => testResults.ToList());

        realResultsAction.ShouldThrow<Exception>();
        testResultsAction.ShouldThrow<Exception>();
    }

    [Fact]
    public async Task GivenAQueryUsingToAnyWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Where(tm => tm.Name == "bob bobertson"));

        var realResultsAction = new Func<object>(() => realResults.Any());
        var testResultsAction = new Func<object>(() => testResults.Any());

        realResultsAction.ShouldThrow<Exception>();
        testResultsAction.ShouldThrow<Exception>();
    }

    [Fact]
    public async Task GivenAQueryUsingFirstOrDefaultWhenExecutingThenBothShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Where(tm => tm.Name == "Bob Bobertson"));

        realResults.Count().ShouldBe(1);
        realResults.ToList().ShouldBeEquivalentTo(testResults.ToList());
    }

    [Fact]
    public async Task GivenAQueryUsingSingleOrDefaultWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
            }
        );

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(q => q.Where(tm => tm.Name == "Bob Bobertson"));

        var realResultsAction = new Func<TestModel?>(() => realResults.SingleOrDefault());
        var testResultsAction = new Func<TestModel?>(() => testResults.SingleOrDefault());

        realResultsAction.ShouldThrow<Exception>();
        testResultsAction.ShouldThrow<Exception>();
    }

    [Fact]
    public async Task GivenAQueryUsingAnyWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(
            new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                Value = false,
            }
        );

        var (_, realException, _, testException) = _testCosmos.WhenExecutingAQuery<TestModel, bool>(q => q.Any());

        realException.ShouldNotBeNull();
        testException.ShouldNotBeNull();
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
