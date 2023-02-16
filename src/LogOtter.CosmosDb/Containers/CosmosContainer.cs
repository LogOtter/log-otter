using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

// ReSharper disable once UnusedTypeParameter
public class CosmosContainer<TDocument>
{
    public Container Container { get; }

    public CosmosContainer(Container container)
    {
        Container = container;
    }
}
