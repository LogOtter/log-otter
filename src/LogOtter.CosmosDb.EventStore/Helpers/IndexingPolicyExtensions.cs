using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.EventStore;

public static class IndexingPolicyExtensions
{
    public static IndexingPolicy WithCompositeIndex(this IndexingPolicy indexingPolicy, params CompositePath[] compositePaths)
    {
        indexingPolicy.CompositeIndexes.Add(new Collection<CompositePath>(compositePaths.ToList()));

        return indexingPolicy;
    }
}
