namespace LogOtter.CosmosDb;

public static class QueryableExtensions
{
    public static IQueryable<T> Page<T>(this IQueryable<T> query, int currentPage, int pageSize)
    {
        if (currentPage < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(currentPage), "Current page must be greater than or equal to 1");
        }
        
        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1");
        }

        return query
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize);
    }
}