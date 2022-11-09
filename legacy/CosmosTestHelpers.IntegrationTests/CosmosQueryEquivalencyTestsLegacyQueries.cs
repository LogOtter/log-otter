using CosmosTestHelpers.IntegrationTests.TestModels;
using FluentAssertions;
using Xunit;

namespace CosmosTestHelpers.IntegrationTests;

[Obsolete("Kept to test legacy querying until the CreateItemLinqQueryable method is removed")]
[Collection("Integration Tests")]
public sealed class CosmosQueryEquivalencyTestsLegacyQueries : IAsyncLifetime, IDisposable
{
    private TestCosmos _testCosmos;

    [Fact]
    public async Task GivenAQueryUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        realResults.Count().Should().Be(1);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingContainsOnAnEnumWithAToStringWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value2
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => new[] { TestEnum.Value2.ToString() }.Contains(tm.EnumValue.ToString()))
        );

        realResults.Count().Should().Be(1);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAValueFromAMethodWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = true
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Value == GetTrue())
        );

        realResults.Count().Should().Be(1);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAValueFromAMethodAgainstModelWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = true,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.GetBoolValue() == true)
        );

        var realResultsAction = new Func<TestModel>(() => realResults.SingleOrDefault());
        var testResultsAction = new Func<TestModel>(() => testResults.SingleOrDefault());

        realResultsAction.Should().Throw<Exception>();
        testResultsAction.Should().Throw<Exception>();
    }

    [Fact]
    public async Task GivenAQueryUsingAValueFromAMethodWithArgsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = true
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Value == GetBool(true))
        );

        realResults.Count().Should().Be(1);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingProjectionWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Select(tm => new TestModel { Name = "Projected" })
        );

        realResults.Count().Should().Be(1);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNotEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Name != "Bobbeta Bobertson")
        );

        realResults.Count().Should().Be(1);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNotXorWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => !(tm.Value ^ false))
        );

        realResults.Count().Should().Be(0);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingXorWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Value ^ true)
        );

        realResults.Count().Should().Be(0);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingToUpperWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Name.ToUpper() == "BOB BOBERTSON")
        );

        realResults.Count().Should().Be(1);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingToLowerWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Name.ToLower() == "bob bobertson")
        );

        realResults.Count().Should().Be(1);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAnyInASubQueryWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Children = new[]
            {
                new SubModel { Value = "bob bobertson" }
            }
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Children.Any(c => c.Value == "bob bobertson"))
        );

        realResults.Count().Should().Be(1);
        realResults.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public async Task GivenAQueryUsingToUpperInvariantWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Name.ToUpperInvariant() == "BOB BOBERTSON")
        );

        var realResultsAction = new Func<IList<TestModel>>(() => realResults.ToList());
        var testResultsAction = new Func<IList<TestModel>>(() => testResults.ToList());

        realResultsAction.Should().Throw<Exception>();
        testResultsAction.Should().Throw<Exception>();
    }

    [Fact]
    public async Task GivenAQueryUsingToLowerInvariantWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Name.ToLowerInvariant() == "bob bobertson")
        );

        var realResultsAction = new Func<IList<TestModel>>(() => realResults.ToList());
        var testResultsAction = new Func<IList<TestModel>>(() => testResults.ToList());

        realResultsAction.Should().Throw<Exception>();
        testResultsAction.Should().Throw<Exception>();
    }

    [Fact]
    public async Task GivenAQueryUsingToAnyWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Name == "bob bobertson")
        );

        var realResultsAction = new Func<bool>(() => realResults.Any());
        var testResultsAction = new Func<bool>(() => testResults.Any());

        realResultsAction.Should().Throw<Exception>();
        testResultsAction.Should().Throw<Exception>();
    }

    [Fact]
    public async Task GivenAQueryUsingFirstOrDefaultWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        var realResultsAction = new Func<TestModel>(() => realResults.FirstOrDefault());
        var testResultsAction = new Func<TestModel>(() => testResults.FirstOrDefault());

        realResultsAction.Should().Throw<Exception>();
        testResultsAction.Should().Throw<Exception>();
    }

    [Fact]
    public async Task GivenAQueryUsingSingleOrDefaultWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (realResults, testResults) = _testCosmos.WhenExecutingAQuery<TestModel>(
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        var realResultsAction = new Func<TestModel>(() => realResults.SingleOrDefault());
        var testResultsAction = new Func<TestModel>(() => testResults.SingleOrDefault());

        realResultsAction.Should().Throw<Exception>();
        testResultsAction.Should().Throw<Exception>();
    }

    [Fact]
    public async Task GivenAQueryUsingAnyWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false
        });

        var (_, realException, _, testException) = _testCosmos.WhenExecutingAQuery<TestModel, bool>(
            q => q.Any()
        );

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
    }

    public Task InitializeAsync()
    {
        _testCosmos = new TestCosmos();
        return _testCosmos.SetupAsync("/partitionKey");
    }

    public async Task DisposeAsync()
    {
        await _testCosmos.CleanupAsync();
    }

    public void Dispose()
    {
        _testCosmos?.Dispose();
    }

    private bool GetTrue() => true;

    private bool GetBool(bool b) => b;
}