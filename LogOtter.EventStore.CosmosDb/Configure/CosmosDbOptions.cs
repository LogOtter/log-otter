using Microsoft.Azure.Cosmos;

namespace LogOtter.EventStore.CosmosDb;

public class CosmosDbOptions
{
    public string ConnectionString { get; set; } = default!;
    public string DatabaseId { get; set; } = default!;
    public CosmosClientOptions? ClientOptions { get; set; }
}