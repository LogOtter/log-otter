using System.Linq.Expressions;
using FluentAssertions;
using LogOtter.CosmosDb.ContainerMock.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosQuerySerializationTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public async Task GivenACustomSerializer_WhenQueryingWithLingSerializationOptions_ShouldRetrieveTheExpectedRecords(
        bool matchOnPropertyUsingASpecifiedName,
        bool useCamelCaseOnBaseSerializer,
        bool useCamelCaseOnQuery,
        bool shouldThrow
    )
    {
        var cosmosSerializationOptions = new CosmosSerializationOptions();
        if (useCamelCaseOnBaseSerializer)
        {
            cosmosSerializationOptions.PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase;
        }

        var containerMock = new ContainerMock(cosmosSerializationOptions: cosmosSerializationOptions);

        await containerMock.CreateItemAsync(
            new TestModel
            {
                Id = "ID",
                Name = "Bob Bobertson",
                Value = false,
                PartitionKey = "PartitionKeyValue"
            }
        );

        var linqSerializerOptions = new CosmosLinqSerializerOptions();
        if (useCamelCaseOnQuery)
        {
            linqSerializerOptions.PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase;
        }

        Expression<Func<TestModel, bool>> predicate = tm => tm.Name == "Bob Bobertson";

        if (matchOnPropertyUsingASpecifiedName)
        {
            predicate = tm => tm.Id == "ID";
        }

        Func<List<TestModel>> act = () =>
            containerMock
                .GetItemLinqQueryable<TestModel>(
                    true,
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey("PartitionKeyValue") },
                    linqSerializerOptions: linqSerializerOptions
                )
                .Where(predicate)
                .ToList();

        if (shouldThrow)
        {
            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("Linq serialization options do not match the base serializer options (Parameter 'linqSerializerOptions')")
                .WithParameterName("linqSerializerOptions");
        }
        else
        {
            act.Should().NotThrow().Which.Should().NotBeEmpty();
        }
    }
}
