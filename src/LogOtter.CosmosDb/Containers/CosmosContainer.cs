using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

// ReSharper disable once UnusedTypeParameter
public class CosmosContainer<TDocument>(Container container)
{
    public Container Container { get; } = container;
}
