using System.Linq.Expressions;
using CustomerApi.NonEventSourcedData.CustomerInterests;
using LogOtter.CosmosDb;
using Microsoft.Azure.Cosmos;
using Shouldly;

// ReSharper disable UnusedMethodReturnValue.Local

namespace CustomerApi.Tests;

public class SearchableInterestStore(CosmosContainer<SearchableInterest> searchableInterestContainer)
{
    private readonly Container _searchableInterestContainer = searchableInterestContainer.Container;

    public async Task ThenTheSearchableInterestShouldMatch(string id, params Action<SearchableInterest>[] conditions)
    {
        var searchableInterest = await _searchableInterestContainer.ReadItemAsync<SearchableInterest>(
            id,
            new PartitionKey(SearchableInterest.StaticPartition)
        );
        searchableInterest.Resource.ShouldNotBeNull();
        searchableInterest.Resource.ShouldSatisfyAllConditions(conditions);
    }
}
