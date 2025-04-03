namespace LogOtter.CosmosDb.Testing;

internal class TestCosmosDatabaseFactory : ICosmosDatabaseFactory
{
    public Task CreateDatabaseIfNotExistsAsync(string databaseId, int? throughput, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
