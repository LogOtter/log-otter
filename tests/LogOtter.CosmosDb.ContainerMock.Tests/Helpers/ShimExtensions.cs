using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace LogOtter.CosmosDb.ContainerMock.Tests.Helpers;

internal static class ShimExtensions
{
    public static async IAsyncEnumerable<TResult> QueryAsync<TModel, TResult>(
        this Container container,
        string? partitionKey,
        Func<IQueryable<TModel>, IQueryable<TResult>> applyQuery
    )
    {
        var queryRequestOptions = new QueryRequestOptions();
        if (partitionKey != null)
        {
            queryRequestOptions.PartitionKey = new PartitionKey(partitionKey);
        }

        var queryable = container.GetItemLinqQueryable<TModel>(requestOptions: queryRequestOptions);
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
                var batch = await feedIterator.ReadNextAsync();

                foreach (var result in batch)
                {
                    yield return result;
                }
            }
        }
    }
}
