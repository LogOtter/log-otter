using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.Testing;

public interface ITestCosmosDbBuilder
{
    public IServiceCollection Services { get; }
}
