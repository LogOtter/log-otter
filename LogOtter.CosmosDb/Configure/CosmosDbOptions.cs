using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

public class CosmosDbOptions
{
    public string ConnectionString { get; set; } = default!;
    public string DatabaseId { get; set; } = default!;
    public string LeasesContainerName { get; set; } = "Leases";
    public CosmosClientOptions? ClientOptions { get; set; }
}