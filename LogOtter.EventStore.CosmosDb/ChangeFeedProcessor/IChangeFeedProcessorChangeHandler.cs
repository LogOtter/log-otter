namespace LogOtter.EventStore.CosmosDb;

public interface IChangeFeedProcessorChangeHandler<TDocument>
{
    Task ProcessChanges(IReadOnlyCollection<TDocument> changes, CancellationToken cancellationToken);
}