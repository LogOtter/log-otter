using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

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

        services.AddSingleton<LogOtterJsonSerializationSettings>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            var cosmosSerializerOptions = options.ClientOptions.SerializerOptions;

            var registeredJsonConverters = sp.GetServices<IRegisteredJsonConverter>();
            var converters = registeredJsonConverters.Select(c => (JsonConverter)sp.GetRequiredService(c.ConverterType)).ToList();

            converters.Add(new StringEnumConverter());

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = cosmosSerializerOptions.IgnoreNullValues ? NullValueHandling.Ignore : NullValueHandling.Include,
                Formatting = cosmosSerializerOptions.Indented ? Formatting.Indented : Formatting.None,
                ContractResolver =
                    cosmosSerializerOptions.PropertyNamingPolicy == CosmosPropertyNamingPolicy.CamelCase
                        ? new CamelCasePropertyNamesContractResolver()
                        : null,
                MaxDepth = 64, // https://github.com/advisories/GHSA-5crp-9r3c-p9vr,
                Converters = converters
            };

            return new LogOtterJsonSerializationSettings(settings, cosmosSerializerOptions.PropertyNamingPolicy);
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            var cosmosClientOptions = options.ClientOptions.UnderlyingOptions;
            cosmosClientOptions.Serializer = new NewtonsoftCustomSerializer(sp.GetRequiredService<LogOtterJsonSerializationSettings>());

            if (options.ManagedIdentityOptions == null)
            {
                return new CosmosClient(options.ConnectionString, cosmosClientOptions);
            }

            var credentialOptions = new DefaultAzureCredentialOptions();
            if (!string.IsNullOrWhiteSpace(options.ManagedIdentityOptions.UserAssignedManagedIdentityClientId))
            {
                credentialOptions.ManagedIdentityClientId = options.ManagedIdentityOptions.UserAssignedManagedIdentityClientId;
            }

            var tokenCredential = new DefaultAzureCredential(credentialOptions);
            return new CosmosClient(options.ManagedIdentityOptions.AccountEndpoint, tokenCredential, cosmosClientOptions);
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
