namespace LogOtter.CosmosDb.EventStore;

/// <summary>
/// Contributes metadata that is written alongside events when they are appended to a stream.
/// Enrichers are invoked once per <c>ApplyEvents</c>/<c>ApplyAndGetEvents</c> call, so every event
/// in a batch shares the same metadata snapshot.
/// </summary>
/// <remarks>
/// Enrichers are registered as singletons (see <c>AddEventMetadataEnricher</c>) and resolved into the
/// singleton <see cref="EventRepository{TBaseEvent,TSnapshot}"/>. To read per-request state, depend on an
/// ambient accessor (e.g. <c>IHttpContextAccessor</c> or <c>Activity.Current</c>) and resolve it inside
/// <see cref="Enrich"/> rather than capturing a scoped service in the constructor.
///
/// Metadata is persisted to Cosmos DB and is readable by anyone with access to the underlying container.
/// Do not write secrets, credentials or sensitive personal data into metadata.
/// </remarks>
public interface IEventMetadataEnricher
{
    void Enrich(IDictionary<string, string> metadata);
}
