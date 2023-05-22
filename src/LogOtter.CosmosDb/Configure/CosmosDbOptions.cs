using System.Net;
using Microsoft.Azure.Cosmos.Fluent;

namespace LogOtter.CosmosDb;

public record CosmosDbOptions
{
    public string ConnectionString { get; set; } = "";
    public string DatabaseId { get; set; } = "";
    public ManagedIdentityOptions? ManagedIdentityOptions { get; set; }
    public ChangeFeedProcessorOptions ChangeFeedProcessorOptions { get; set; } = new();
    public ClientOptions ClientOptions { get; set; } = new();
}
