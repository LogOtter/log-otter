using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb;

public static class ConfigureExtensions
{
    public static ICosmosDbBuilder AddCosmosDb(this IServiceCollection serviceCollection, Action<CosmosDbOptions>? setupAction = null)
    {
        if (setupAction != null)
        {
            serviceCollection.Configure(setupAction);
        }

        serviceCollection.AddSingleton<IValidateOptions<CosmosDbOptions>, CosmosDbOptionsValidator>();

        serviceCollection.AddSingleton(
            sp =>
            {
                var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

                if (options.ManagedIdentityOptions == null)
                {
                    return new CosmosClient(options.ConnectionString, options.ClientOptions);
                }

                var credentialOptions = new DefaultAzureCredentialOptions();
                if (!string.IsNullOrWhiteSpace(options.ManagedIdentityOptions.UserAssignedManagedIdentityClientId))
                {
                    credentialOptions.ManagedIdentityClientId = options.ManagedIdentityOptions.UserAssignedManagedIdentityClientId;
                }

                var tokenCredential = new DefaultAzureCredential(credentialOptions);
                return new CosmosClient(options.ManagedIdentityOptions.AccountEndpoint, tokenCredential, options.ClientOptions);
            });

        serviceCollection.AddSingleton(
            sp =>
            {
                var cosmosDbOptions = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
                var client = sp.GetRequiredService<CosmosClient>();

                var response = client.CreateDatabaseIfNotExistsAsync(cosmosDbOptions.DatabaseId).GetAwaiter().GetResult();

                return response.Database;
            });

        serviceCollection.AddSingleton<IFeedIteratorFactory, FeedIteratorFactory>();
        serviceCollection.AddSingleton<ICosmosContainerFactory, CosmosContainerFactory>();
        serviceCollection.AddSingleton<IChangeFeedProcessorFactory, ChangeFeedProcessorFactory>();

        serviceCollection.AddHostedService<ChangeFeedProcessorRunnerService>();

        return new CosmosDbBuilder(serviceCollection);
    }
}
