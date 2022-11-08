using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.EventStore.Testing;

public interface ITestEventStoreBuilder
{
    IServiceCollection Services { get; }
}