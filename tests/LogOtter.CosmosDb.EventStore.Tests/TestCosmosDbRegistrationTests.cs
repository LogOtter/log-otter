using LogOtter.CosmosDb.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class TestCosmosDbRegistrationTests
{
    [Fact]
    public void CallingAddTestCosmosDbBeforeAddCosmosDbThrows()
    {
        var services = new ServiceCollection();

        var exception = Should.Throw<InvalidOperationException>(() => services.AddTestCosmosDb());

        exception.Message.ShouldBe(
            $"{nameof(Testing.ConfigureExtensions.AddTestCosmosDb)} must be called after {nameof(CosmosDb.ConfigureExtensions.AddCosmosDb)}, so that it can replace the live config's Cosmos DB services with test ones. Make sure the live config is set up before calling {nameof(Testing.ConfigureExtensions.AddTestCosmosDb)} (e.g. by calling it from ConfigureTestServices when using WebApplicationFactory)."
        );
    }
}
