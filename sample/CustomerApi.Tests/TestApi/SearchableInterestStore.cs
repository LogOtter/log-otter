﻿using System.Linq.Expressions;
using Bogus;
using CustomerApi.Events.Customers;
using CustomerApi.NonEventSourcedData.CustomerInterests;
using CustomerApi.Services;
using CustomerApi.Uris;
using FluentAssertions;
using LogOtter.CosmosDb;
using LogOtter.CosmosDb.EventStore;
using Microsoft.Azure.Cosmos;

// ReSharper disable UnusedMethodReturnValue.Local

namespace CustomerApi.Tests;

public class SearchableInterestStore(CosmosContainer<SearchableInterest> searchableInterestContainer)
{
    private readonly Container _searchableInterestContainer = searchableInterestContainer.Container;

    public async Task ThenTheSearchableInterestShouldMatch(string id, Expression<Func<SearchableInterest, bool>> matchFunc)
    {
        var searchableInterest = await _searchableInterestContainer.ReadItemAsync<SearchableInterest>(
            id,
            new PartitionKey(SearchableInterest.StaticPartition)
        );
        searchableInterest.Resource.Should().NotBeNull();
        searchableInterest.Resource.Should().Match(matchFunc);
    }
}
