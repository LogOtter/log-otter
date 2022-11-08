using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.Testing;

public class TestCosmosDbBuilder : ITestCosmosDbBuilder
{
    public IServiceCollection Services { get; }

    public TestCosmosDbBuilder(IServiceCollection services)
    {
        Services = services;
    }
}