using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

public record CosmosDbOptions
{
    public string ConnectionString { get; set; } = "";
    public string DatabaseId { get; set; } = "";
    public ChangeFeedProcessorOptions ChangeFeedProcessorOptions { get; set; } = new();
    public CosmosClientOptions? ClientOptions { get; set; }
}