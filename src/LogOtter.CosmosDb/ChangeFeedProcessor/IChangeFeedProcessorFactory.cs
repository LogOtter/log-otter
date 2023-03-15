using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

public interface IChangeFeedProcessorFactory
{
    IChangeFeedProcessor
        CreateChangeFeedProcessor<TRawDocument, TDocument, TChangeFeedHandlerDocument, TChangeFeedChangeConverter, TChangeFeedProcessorHandler>(
            string processorName,
            Container documentContainer,
            Func<IServiceProvider, Task<bool>>? enabledFunc,
            int batchSize,
            DateTime? activationDate)
        where TChangeFeedProcessorHandler : IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument>
        where TChangeFeedChangeConverter : IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument>;
}
