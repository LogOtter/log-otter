using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb;

public interface ICosmosDbBuilder
{
    public IServiceCollection Services { get; }

    ICosmosDbBuilder AddContainer<TDocument>(
        string containerName,
        string partitionKeyPath = "/partitionKey",
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = null,
        IEnumerable<Collection<CompositePath>>? compositeIndexes = null,
        ThroughputProperties? throughputProperties = null
    );

    ICosmosDbBuilder AddChangeFeedProcessor<TDocument, TChangeFeedProcessorHandler>(
        string processorName,
        Func<IServiceProvider, Task<bool>>? enabledFunc = null,
        int batchSize = 100,
        DateTime? activationDate = null
    ) where TChangeFeedProcessorHandler : class, IChangeFeedProcessorChangeHandler<TDocument>;

    ICosmosDbBuilder AddChangeFeedProcessor<
        TRawDocument,
        TDocument,
        TChangeFeedChangeConverter,
        TChangeFeedProcessorHandler
    >(
        string processorName,
        Func<IServiceProvider, Task<bool>>? enabledFunc = null,
        int batchSize = 100,
        DateTime? activationDate = null
    )
        where TChangeFeedChangeConverter : class, IChangeFeedChangeConverter<TRawDocument, TDocument>
        where TChangeFeedProcessorHandler : class, IChangeFeedProcessorChangeHandler<TDocument>;

    ICosmosDbBuilder AddChangeFeedProcessor<
        TRawDocument,
        TDocument,
        TChangeFeedHandlerDocument,
        TChangeFeedChangeConverter,
        TChangeFeedProcessorHandler
    >(
        string processorName,
        Func<IServiceProvider, Task<bool>>? enabledFunc = null,
        int batchSize = 100,
        DateTime? activationDate = null
    )
        where TChangeFeedChangeConverter : class, IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument>
        where TChangeFeedProcessorHandler : class, IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument>;
}