namespace LogOtter.EventStore.CosmosDb;

public class NoChangeFeedChangeConverter<TDocument> : IChangeFeedChangeConverter<TDocument, TDocument>
{
    public TDocument ConvertChange(TDocument change)
    {
        return change;
    }
}