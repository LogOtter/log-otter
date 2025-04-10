using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

internal class CosmosDatabaseFactory(CosmosClient cosmosClient) : ICosmosDatabaseFactory
{
    public async Task CreateDatabaseIfNotExistsAsync(string databaseId, int? throughput, CancellationToken cancellationToken)
    {
        await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId, throughput, cancellationToken: cancellationToken);
    }
}
