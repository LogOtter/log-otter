using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[CollectionDefinition("Integration Tests")]
public class IntegrationTestsCollection : ICollectionFixture<IntegrationTestsFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}