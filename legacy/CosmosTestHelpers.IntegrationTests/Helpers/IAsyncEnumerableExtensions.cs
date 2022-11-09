namespace CosmosTestHelpers.IntegrationTests;

internal static class AsyncEnumerableExtensions
{
    public static async Task<IList<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable)
    {
        var result = new List<T>();
        await foreach (var item in enumerable)
        {
            result.Add(item);
        }

        return result;
    }  
}