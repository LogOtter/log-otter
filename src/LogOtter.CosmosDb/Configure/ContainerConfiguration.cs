using LogOtter.CosmosDb.Metadata;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

public class ContainerConfiguration<TDocument>
{
    internal AutoProvisionMetadata? AutoProvisionMetadata { get; private set; }

    internal List<ChangeFeedProcessorMetadata> ChangeFeedProcessorsMetadata { get; }

    internal HashSet<Type> SubTypes { get; }

    internal ContainerConfiguration()
    {
        ChangeFeedProcessorsMetadata = new List<ChangeFeedProcessorMetadata>();
        SubTypes = new HashSet<Type>();
    }

    public ContainerConfiguration<TDocument> WithAutoProvisionSettings(
        string partitionKeyPath = "/partitionKey",
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = -1,
        IndexingPolicy? indexingPolicy = null,
        ThroughputProperties? throughputProperties = null
    )
    {
        AutoProvisionMetadata = new AutoProvisionMetadata(partitionKeyPath, uniqueKeyPolicy, defaultTimeToLive, indexingPolicy, throughputProperties);

        return this;
    }

    public ContainerConfiguration<TDocument> WithChangeFeedProcessor<TChangeFeedProcessorHandler>(
        string processorName,
        Func<IServiceProvider, Task<bool>>? enabledFunc = null,
        int batchSize = 100,
        DateTime? activationDate = null
    )
        where TChangeFeedProcessorHandler : class, IChangeFeedProcessorChangeHandler<TDocument>
    {
        return WithChangeFeedProcessor<TDocument, TDocument, NoChangeFeedChangeConverter<TDocument>, TChangeFeedProcessorHandler>(
            processorName,
            enabledFunc,
            batchSize,
            activationDate
        );
    }

    public ContainerConfiguration<TDocument> WithChangeFeedProcessor<TRawDocument, TChangeFeedChangeConverter, TChangeFeedProcessorHandler>(
        string processorName,
        Func<IServiceProvider, Task<bool>>? enabledFunc = null,
        int batchSize = 100,
        DateTime? activationDate = null
    )
        where TChangeFeedChangeConverter : class, IChangeFeedChangeConverter<TRawDocument, TDocument>
        where TChangeFeedProcessorHandler : class, IChangeFeedProcessorChangeHandler<TDocument>
    {
        return WithChangeFeedProcessor<TRawDocument, TDocument, TChangeFeedChangeConverter, TChangeFeedProcessorHandler>(
            processorName,
            enabledFunc,
            batchSize,
            activationDate
        );
    }

    public ContainerConfiguration<TDocument> WithChangeFeedProcessor<
        TRawDocument,
        TChangeFeedHandlerDocument,
        TChangeFeedChangeConverter,
        TChangeFeedProcessorHandler
    >(string processorName, Func<IServiceProvider, Task<bool>>? enabledFunc = null, int batchSize = 100, DateTime? activationDate = null)
        where TChangeFeedChangeConverter : class, IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument>
        where TChangeFeedProcessorHandler : class, IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument>
    {
        var metadata = new ChangeFeedProcessorMetadata(
            typeof(TRawDocument),
            typeof(TDocument),
            typeof(TChangeFeedHandlerDocument),
            typeof(TChangeFeedChangeConverter),
            typeof(TChangeFeedProcessorHandler),
            processorName,
            enabledFunc,
            batchSize,
            activationDate
        );

        ChangeFeedProcessorsMetadata.Add(metadata);

        return this;
    }

    public ContainerConfiguration<TDocument> WithSubTypeThatMustHaveDistinctPartition<T>()
        where T : TDocument
    {
        SubTypes.Add(typeof(T));
        return this;
    }
}
