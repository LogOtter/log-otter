using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

public class CosmosDbOptions
{
    public string ConnectionString { get; set; } = default!;
    public string DatabaseId { get; set; } = default!;
    public ChangeFeedProcessorOptions ChangeFeedProcessorOptions { get; set; } = new ChangeFeedProcessorOptions();
    public CosmosClientOptions? ClientOptions { get; set; }
}