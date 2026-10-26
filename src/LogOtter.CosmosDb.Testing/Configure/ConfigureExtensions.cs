using LogOtter.CosmosDb.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.Testing;

public static class ConfigureExtensions
{
    public static ITestCosmosDbBuilder AddTestCosmosDb(this IServiceCollection services)
    {
        services.AddSingleton<IFeedIteratorFactory, TestFeedIteratorFactory>();
        services.AddSingleton(sp =>
        {
            PreProvisionedContainerValidator.Validate(sp.GetServices<PreProvisionedContainer>(), sp.GetServices<ContainerAutoProvisionMetadata>());
            return new TestCosmosContainerFactory();
        });
        services.AddSingleton<ICosmosDatabaseFactory, TestCosmosDatabaseFactory>();
        services.AddSingleton<ICosmosContainerFactory>(sp => sp.GetRequiredService<TestCosmosContainerFactory>());
        services.AddSingleton<IChangeFeedProcessorFactory, TestChangeFeedProcessorFactory>();

        return new TestCosmosDbBuilder(services);
    }
}
