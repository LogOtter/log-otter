using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.EventStore.Testing;

public class TestEventStoreBuilder : ITestEventStoreBuilder
{
    public IServiceCollection Services { get; }

    public TestEventStoreBuilder(IServiceCollection services)
    {
        Services = services;
    }
}