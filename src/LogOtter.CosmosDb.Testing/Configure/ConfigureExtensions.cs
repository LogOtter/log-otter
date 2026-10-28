using LogOtter.CosmosDb.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.Testing;

public static class ConfigureExtensions
{
    public static ITestCosmosDbBuilder AddTestCosmosDb(this IServiceCollection services)
    {
        if (!services.IsCosmosDbAdded())
        {
            throw new InvalidOperationException(
                $"{nameof(AddTestCosmosDb)} must be called after {nameof(CosmosDb.ConfigureExtensions.AddCosmosDb)}, so that it can replace the live config's Cosmos DB services with test ones. Make sure the live config is set up before calling {nameof(AddTestCosmosDb)} (e.g. by calling it from ConfigureTestServices when using WebApplicationFactory)."
            );
        }

        RemoveConnectionOptionsValidation(services);

        services.AddSingleton<IFeedIteratorFactory, TestFeedIteratorFactory>();
        services.AddSingleton(sp =>
        {
            var preProvisionedContainers = sp.GetServices<PreProvisionedContainer>().ToList();
            var registeredContainers = sp.GetServices<ContainerAutoProvisionMetadata>().ToList();
            var autoProvisioningEnabled = sp.GetService<AutoProvisionSettings>()?.Enabled == true;

            PreProvisionedContainerValidator.Validate(preProvisionedContainers, registeredContainers, autoProvisioningEnabled);
            return new TestCosmosContainerFactory(preProvisionedContainers, autoProvisioningEnabled ? registeredContainers : []);
        });
        services.AddSingleton<ICosmosDatabaseFactory, TestCosmosDatabaseFactory>();
        services.AddSingleton<ICosmosContainerFactory>(sp => sp.GetRequiredService<TestCosmosContainerFactory>());
        services.AddSingleton<IChangeFeedProcessorFactory, TestChangeFeedProcessorFactory>();

        return new TestCosmosDbBuilder(services);
    }

    private static void RemoveConnectionOptionsValidation(IServiceCollection services)
    {
        var optionsValidator = services.FirstOrDefault(s => s.ImplementationType == typeof(CosmosDbOptionsValidator));

        if (optionsValidator != null)
        {
            services.Remove(optionsValidator);
        }
    }
}
