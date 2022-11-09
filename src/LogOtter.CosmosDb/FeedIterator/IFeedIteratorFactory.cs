using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

public interface IFeedIteratorFactory
{
    FeedIterator<T> GetFeedIterator<T>(IQueryable<T> query);
}