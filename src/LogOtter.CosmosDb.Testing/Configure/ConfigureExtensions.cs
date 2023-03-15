using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.Testing;

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
