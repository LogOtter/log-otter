using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Testing;

public class TestFeedIteratorFactory : IFeedIteratorFactory
{
    public FeedIterator<T> GetFeedIterator<T>(IQueryable<T> query)
    {
        return new TestFeedIterator<T>(query);
    }
}
