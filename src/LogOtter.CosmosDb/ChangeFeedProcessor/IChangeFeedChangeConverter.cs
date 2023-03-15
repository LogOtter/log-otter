namespace LogOtter.CosmosDb;

public interface IChangeFeedChangeConverter<TRawDocument, TDocument>
{
    TDocument ConvertChange(TRawDocument change);
}
