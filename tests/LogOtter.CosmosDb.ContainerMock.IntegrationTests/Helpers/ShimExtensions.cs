﻿using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

internal static class ShimExtensions
{
    private static readonly RetryPolicy RetryPolicy = new(new Logger<RetryPolicy>(new LoggerFactory()));

    public static Task<int> CountAsync(this Container container, string? partitionKey)
    {
        return CountAsync<object>(container, partitionKey, q => q);
    }

    public static async Task<int> CountAsync<TModel>(
        this Container container,
        string? partitionKey,
        Func<IQueryable<TModel>, IQueryable<TModel>> applyQuery
    )
    {
        if (container is ContainerMock mock)
        {
            return await mock.CountAsync(partitionKey, applyQuery);
        }

        var queryRequestOptions = new QueryRequestOptions();
        if (partitionKey != null)
        {
            queryRequestOptions.PartitionKey = new PartitionKey(partitionKey);
        }

        var queryable = container.GetItemLinqQueryable<TModel>(requestOptions: queryRequestOptions);

        var projectedQuery = applyQuery(queryable);

        var result = await RetryPolicy.CreateAndExecutePolicyAsync(nameof(CountAsync), async () => await projectedQuery.CountAsync());

        return result;
    }

    public static IAsyncEnumerable<TModel> QueryAsync<TModel>(
        this Container container,
        string? partitionKey,
        Func<IQueryable<TModel>, IQueryable<TModel>> applyQuery,
        CosmosLinqSerializerOptions? linqSerializerOptions = null
    )
    {
        return QueryAsync<TModel, TModel>(container, partitionKey, applyQuery, linqSerializerOptions);
    }

    public static IAsyncEnumerable<TModel> QueryAsync<TModel>(
        this Container container,
        string? partitionKey,
        CosmosLinqSerializerOptions? linqSerializerOptions = null
    )
    {
        return QueryAsync<TModel, TModel>(container, partitionKey, q => q, linqSerializerOptions);
    }

    public static async IAsyncEnumerable<TResult> QueryAsync<TModel, TResult>(
        this Container container,
        string? partitionKey,
        Func<IQueryable<TModel>, IQueryable<TResult>> applyQuery,
        CosmosLinqSerializerOptions? linqSerializerOptions
    )
    {
        var queryRequestOptions = new QueryRequestOptions();
        if (partitionKey != null)
        {
            queryRequestOptions.PartitionKey = new PartitionKey(partitionKey);
        }

        var queryable = container.GetItemLinqQueryable<TModel>(requestOptions: queryRequestOptions, linqSerializerOptions: linqSerializerOptions);
        var projectedQuery = applyQuery(queryable);

        if (container is ContainerMock)
        {
            foreach (var result in projectedQuery)
            {
                yield return result;
            }
        }
        else
        {
            using var feedIterator = projectedQuery.ToFeedIterator();

            while (feedIterator.HasMoreResults)
            {
                var batch = await RetryPolicy.CreateAndExecutePolicyAsync(nameof(QueryAsync), async () => await feedIterator.ReadNextAsync());

                foreach (var result in batch)
                {
                    yield return result;
                }
            }
        }
    }
}
