using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb;

public static class ConfigureExtensions
{
    public static CosmosDbBuilder AddCosmosDb(this IServiceCollection services, Action<CosmosDbOptions>? setupAction = null)
    {
        if (setupAction != null)
        {
            services.Configure(setupAction);
        }

        services.AddSingleton<IValidateOptions<CosmosDbOptions>, CosmosDbOptionsValidator>();
        services.AddSingleton(_ => new AutoProvisionSettings(false));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

            options.ClientOptions ??= new CosmosClientOptions
            {
                // Hrmmm I'm not sure how to set the serializer TypeNameHandling...
            };

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

        services.AddSingleton(sp =>
        {
            var cosmosDbOptions = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            var client = sp.GetRequiredService<CosmosClient>();

            var autoProvisionSettings = sp.GetRequiredService<AutoProvisionSettings>();

            var database = autoProvisionSettings.Enabled
                ? client.CreateDatabaseIfNotExistsAsync(cosmosDbOptions.DatabaseId).GetAwaiter().GetResult().Database
                : client.GetDatabase(cosmosDbOptions.DatabaseId);

            return database;
        });

        services.AddSingleton<IFeedIteratorFactory, FeedIteratorFactory>();
        services.AddSingleton<ICosmosContainerFactory, CosmosContainerFactory>();
        services.AddSingleton<IChangeFeedProcessorFactory, ChangeFeedProcessorFactory>();

        services.AddHostedService<ChangeFeedProcessorRunnerService>();

        return new CosmosDbBuilder(services);
    }
}
