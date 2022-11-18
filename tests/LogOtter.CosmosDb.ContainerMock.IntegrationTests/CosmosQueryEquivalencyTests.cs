using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos.Linq;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosQueryEquivalencyTests : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos;

    public CosmosQueryEquivalencyTests(IntegrationTestsFixture testFixture)
    {
        _testCosmos = testFixture.CreateTestCosmos();
    }

    [Fact]
    public async Task GivenAQueryUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task IsNullWorks()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = null,
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name.IsNull())
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task IsDefinedWorksOnNull()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = null,
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name.IsDefined())
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenACountUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD2",
            Name = "Bobetta Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenCountingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        realResults.Should().Be(1);
        testResults.Should().Be(1);
    }

    [Fact]
    public async Task GivenAQueryForAllItemsInAPartitionUsingEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>("partition");

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingContainsOnAnEnumWithAToStringWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            EnumValue = TestEnum.Value2,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => new[] { TestEnum.Value2.ToString() }.Contains(tm.EnumValue.ToString()))
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAValueFromAMethodWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = true,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Value == GetTrue())
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingAValueFromAMethodWithArgsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = true,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Value == GetBool(true))
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
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

        Func<Task> action = () => _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.GetBoolValue() == true)
        );

        var exceptionAssertions = await action.Should().ThrowAsync<CosmosEquivalencyException>();
        exceptionAssertions.Which.RealException.Should().NotBeNull();
        exceptionAssertions.Which.TestException.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenAQueryUsingProjectionWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Select(tm => new TestModel { Name = "Projected" })
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNotEqualsWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name != "Bobbeta Bobertson")
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingNotXorWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => !(tm.Value ^ false))
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(0);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingXorWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Value ^ true)
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(0);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingToUpperWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name.ToUpper() == "BOB BOBERTSON")
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingToLowerWhenExecutingThenTheResultsShouldMatch()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name.ToLower() == "bob bobertson")
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
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
            },
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Children.Any(c => c.Value == "bob bobertson"))
        );

        realResults.Should().NotBeNull();
        realResults!.Count.Should().Be(1);
        testResults.Should().BeEquivalentTo(realResults);
    }

    [Fact]
    public async Task GivenAQueryUsingToUpperInvariantWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        Func<Task> action = () => _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name.ToUpperInvariant() == "BOB BOBERTSON")
        );

        var exceptionAssertions = await action.Should().ThrowAsync<CosmosEquivalencyException>();
        exceptionAssertions.Which.RealException.Should().NotBeNull();
        exceptionAssertions.Which.TestException.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenAQueryUsingToLowerInvariantWhenExecutingThenBothShouldError()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        Func<Task> action = () => _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name.ToLowerInvariant() == "bob bobertson")
        );

        var exceptionAssertions = await action.Should().ThrowAsync<CosmosEquivalencyException>();
        exceptionAssertions.Which.RealException.Should().NotBeNull();
        exceptionAssertions.Which.TestException.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenAQueryUsingToAnyWhenExecutingThenBothShouldWork()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name == "bob bobertson")
        );

        realResults.Should().NotBeNull();
        testResults.Should().NotBeNull();
        
        testResults!.Any().Should().Be(realResults!.Any());
    }

    [Fact]
    public async Task GivenAQueryUsingFirstOrDefaultWhenExecutingThenBothShouldWork()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        realResults.Should().NotBeNull();
        testResults.Should().NotBeNull();
        
        realResults!.FirstOrDefault().Should().NotBeNull();
        testResults!.FirstOrDefault().Should().BeEquivalentTo(realResults.FirstOrDefault());
    }

    [Fact]
    public async Task GivenAQueryUsingSingleOrDefaultWhenExecutingThenBothShouldWork()
    {
        await _testCosmos.GivenAnExistingItem(new TestModel
        {
            Id = "RECORD1",
            Name = "Bob Bobertson",
            Value = false,
            PartitionKey = "partition"
        });

        var (realResults, testResults) = await _testCosmos.WhenExecutingAQuery<TestModel>(
            "partition",
            q => q.Where(tm => tm.Name == "Bob Bobertson")
        );

        realResults.Should().NotBeNull();
        testResults.Should().NotBeNull();
        
        realResults!.SingleOrDefault().Should().NotBeNull();
        testResults!.SingleOrDefault().Should().BeEquivalentTo(realResults!.SingleOrDefault());
    }

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

    private bool GetTrue() => true;

    private bool GetBool(bool b) => b;
}