using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.Tests;

public class ContainerRegistrationTests
{
    [Fact]
    public void CallingAddCosmosDbMoreThanOnceThrows()
    {
        var services = new ServiceCollection();
        services.AddCosmosDb();

        var exception = Should.Throw<InvalidOperationException>(() => services.AddCosmosDb());

        exception.Message.ShouldBe(
            $"{nameof(ConfigureExtensions.AddCosmosDb)} can only be called once as multiple databases are not supported. Register all containers using the {nameof(CosmosDbBuilder)} returned by the first call to {nameof(ConfigureExtensions.AddCosmosDb)}."
        );
    }

    [Fact]
    public void RegisteringATypeAsASubTypeOfItselfThrows()
    {
        var builder = new ServiceCollection().AddCosmosDb();

        var exception = Should.Throw<InvalidOperationException>(() =>
            builder.AddContainer<BaseDocument>("Documents", c => c.WithSubTypeThatMustHaveDistinctPartition<BaseDocument>())
        );

        exception.Message.ShouldBe($"{nameof(BaseDocument)} cannot be registered as a subtype of itself");
    }

    [Fact]
    public void RegisteringASubTypeThatIsAlreadyRegisteredWithAContainerThrows()
    {
        var builder = new ServiceCollection().AddCosmosDb().AddContainer<SubDocument>("SubDocuments");

        var exception = Should.Throw<InvalidOperationException>(() =>
            builder.AddContainer<BaseDocument>("Documents", c => c.WithSubTypeThatMustHaveDistinctPartition<SubDocument>())
        );

        exception.Message.ShouldBe(
            $"{nameof(SubDocument)} cannot be registered as a subtype of {nameof(BaseDocument)} as it is already registered with container SubDocuments"
        );
    }

    [Fact]
    public void RegisteringAContainerForATypeThatIsAlreadyRegisteredAsASubTypeThrows()
    {
        var builder = new ServiceCollection()
            .AddCosmosDb()
            .AddContainer<BaseDocument>("Documents", c => c.WithSubTypeThatMustHaveDistinctPartition<SubDocument>());

        var exception = Should.Throw<InvalidOperationException>(() => builder.AddContainer<SubDocument>("SubDocuments"));

        exception.Message.ShouldBe(
            $"Container for {nameof(SubDocument)} cannot be registered as it is already registered as a subtype of {nameof(BaseDocument)} in container Documents"
        );
    }

    [Fact]
    public void RegisteringASubTypeThatIsAlreadyRegisteredAsASubTypeInAnotherContainerThrows()
    {
        var builder = new ServiceCollection()
            .AddCosmosDb()
            .AddContainer<BaseDocument>("Documents", c => c.WithSubTypeThatMustHaveDistinctPartition<SubSubDocument>());

        var exception = Should.Throw<InvalidOperationException>(() =>
            builder.AddContainer<SubDocument>("SubDocuments", c => c.WithSubTypeThatMustHaveDistinctPartition<SubSubDocument>())
        );

        exception.Message.ShouldBe(
            $"{nameof(SubSubDocument)} cannot be registered as a subtype of {nameof(SubDocument)} as it is already registered as a subtype of {nameof(BaseDocument)} in container Documents"
        );
    }

    private class BaseDocument;

    private class SubDocument : BaseDocument;

    private class SubSubDocument : SubDocument;
}
