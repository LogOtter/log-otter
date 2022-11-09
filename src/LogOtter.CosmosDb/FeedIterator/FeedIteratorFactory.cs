using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace LogOtter.CosmosDb;

public class FeedIteratorFactory : IFeedIteratorFactory
{
    public FeedIterator<T> GetFeedIterator<T>(IQueryable<T> query)
    {
        return query.ToFeedIterator();
    }
}