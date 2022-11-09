using LogOtter.CosmosDb;
using LogOtter.CosmosDb.Testing;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureExtensions
{
    public static ITestCosmosDbBuilder AddTestCosmosDb(this IServiceCollection services)
    {
        services.AddSingleton<IFeedIteratorFactory, TestFeedIteratorFactory>();
        services.AddSingleton<TestCosmosContainerFactory>();
        services.AddSingleton<ICosmosContainerFactory>(sp => sp.GetRequiredService<TestCosmosContainerFactory>());
        services.AddSingleton<IChangeFeedProcessorFactory, TestChangeFeedProcessorFactory>();

        return new TestCosmosDbBuilder(services);
    }
}