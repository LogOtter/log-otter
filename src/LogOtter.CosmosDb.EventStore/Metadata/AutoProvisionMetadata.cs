using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.EventStore.Metadata;

public record AutoProvisionMetadata(IReadOnlyCollection<Collection<CompositePath>> CompositeIndexes);
