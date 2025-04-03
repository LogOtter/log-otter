using Azure.Identity;
using LogOtter.CosmosDb.Metadata;
using LogOtter.CosmosDb.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
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
        services.AddSingleton<ContainerCatalog>();

        services.AddSingleton(sp =>
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

        services.AddSingleton(sp =>
        {
            var cosmosDbOptions = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            var client = sp.GetRequiredService<CosmosClient>();

            return client.GetDatabase(cosmosDbOptions.DatabaseId);
        });

        services.AddSingleton<IFeedIteratorFactory, FeedIteratorFactory>();
        services.AddSingleton<ICosmosDatabaseFactory, CosmosDatabaseFactory>();
        services.AddSingleton<ICosmosContainerFactory, CosmosContainerFactory>();
        services.AddSingleton<IChangeFeedProcessorFactory, ChangeFeedProcessorFactory>();

        services.AddHostedService<StartupService>();
        services
            .AddHealthChecks()
            .Add(
                new HealthCheckRegistration(
                    "LogOtterCosmosAutoProvision",
                    sp => sp.GetHostedService<StartupService>(),
                    null,
                    ["LogOtter", "CosmosDb"]
                )
            );

        return new CosmosDbBuilder(services);
    }

    public static async Task ProvisionCosmosDb(this IHost host, CancellationToken cancellationToken = default)
    {
        var startupService = host.Services.GetHostedService<StartupService>();
        await startupService.ProvisionCosmosDb(cancellationToken);
    }

    private static T GetHostedService<T>(this IServiceProvider serviceProvider)
    {
        return (T)serviceProvider.GetServices<IHostedService>().Single(t => t is T);
    }
}
