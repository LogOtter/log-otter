namespace LogOtter.CosmosDb;

public interface ICosmosDatabaseFactory
{
    Task CreateDatabaseIfNotExistsAsync(string databaseId, int? throughput, CancellationToken cancellationToken);
}
