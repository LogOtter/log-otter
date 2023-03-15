using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Testing;

public class TestFeedIterator<T> : FeedIterator<T>
{
    private readonly Queue<T> _results;

    public override bool HasMoreResults => _results.Any();

    public TestFeedIterator(IQueryable<T> query)
    {
        _results = new Queue<T>();
        foreach (var result in query.ToList())
        {
            _results.Enqueue(result);
        }
    }

    public override Task<FeedResponse<T>> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        var batch = new List<T> { _results.Dequeue() };
        var response = new TestFeedResponse<T>(batch);

        return Task.FromResult<FeedResponse<T>>(response);
    }
}
