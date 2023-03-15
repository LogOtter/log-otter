using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb;

internal class CosmosDbBuilder : ICosmosDbBuilder
{
    private readonly IList<string> _changeFeedProcessors;
    private readonly IDictionary<Type, string> _containers;

    public CosmosDbBuilder(IServiceCollection serviceCollection)
    {
        _containers = new Dictionary<Type, string>();
        _changeFeedProcessors = new List<string>();

        Services = serviceCollection;
    }

    public IServiceCollection Services { get; }

    public ICosmosDbBuilder AddContainer<T>(
        string containerName,
        string partitionKeyPath = "/partitionKey",
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = null,
        IEnumerable<Collection<CompositePath>>? compositeIndexes = null,
        ThroughputProperties? throughputProperties = null)
    {
        RegisterContainer<T>(containerName);

        Services.AddSingleton(
            sp =>
            {
                var cosmosContainerFactory = sp.GetRequiredService<ICosmosContainerFactory>();

                var container = cosmosContainerFactory.CreateContainerIfNotExistsAsync(
                                                          containerName,
                                                          partitionKeyPath,
                                                          uniqueKeyPolicy,
                                                          defaultTimeToLive,
                                                          compositeIndexes,
                                                          throughputProperties)
                                                      .GetAwaiter()
                                                      .GetResult();

                return new CosmosContainer<T>(container);
            });

        return this;
    }

    public ICosmosDbBuilder AddChangeFeedProcessor<TDocument, TChangeFeedProcessorHandler>(
        string processorName,
        Func<IServiceProvider, Task<bool>>? enabledFunc = null,
        int batchSize = 100,
        DateTime? activationDate = null) where TChangeFeedProcessorHandler : class, IChangeFeedProcessorChangeHandler<TDocument>
    {
        return AddChangeFeedProcessor<TDocument, TDocument, TDocument, NoChangeFeedChangeConverter<TDocument>, TChangeFeedProcessorHandler>(
            processorName,
            enabledFunc,
            batchSize,
            activationDate);
    }

    public ICosmosDbBuilder AddChangeFeedProcessor<TRawDocument, TDocument, TChangeFeedChangeConverter, TChangeFeedProcessorHandler>(
        string processorName,
        Func<IServiceProvider, Task<bool>>? enabledFunc = null,
        int batchSize = 100,
        DateTime? activationDate = null)
        where TChangeFeedChangeConverter : class, IChangeFeedChangeConverter<TRawDocument, TDocument>
        where TChangeFeedProcessorHandler : class, IChangeFeedProcessorChangeHandler<TDocument>
    {
        return AddChangeFeedProcessor<TRawDocument, TDocument, TDocument, TChangeFeedChangeConverter, TChangeFeedProcessorHandler>(
            processorName,
            enabledFunc,
            batchSize,
            activationDate);
    }

    public ICosmosDbBuilder
        AddChangeFeedProcessor<TRawDocument, TDocument, TChangeFeedHandlerDocument, TChangeFeedChangeConverter, TChangeFeedProcessorHandler>(
            string processorName,
            Func<IServiceProvider, Task<bool>>? enabledFunc = null,
            int batchSize = 100,
            DateTime? activationDate = null)
        where TChangeFeedChangeConverter : class, IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument>
        where TChangeFeedProcessorHandler : class, IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument>
    {
        RegisterChangeFeedProcessor(processorName);

        var documentType = typeof(TDocument);

        if (!_containers.ContainsKey(documentType))
        {
            throw new InvalidOperationException($"Container for {documentType.Name} has not be registered. Use .AddContainer() to register it.");
        }

        Services.AddSingleton<TChangeFeedChangeConverter>();
        Services.AddSingleton<TChangeFeedProcessorHandler>();

        Services.AddTransient(
            sp =>
            {
                var container = sp.GetRequiredService<CosmosContainer<TDocument>>();
                var changeFeedProcessorFactory = sp.GetRequiredService<IChangeFeedProcessorFactory>();

                return changeFeedProcessorFactory
                    .CreateChangeFeedProcessor<TRawDocument, TDocument, TChangeFeedHandlerDocument, TChangeFeedChangeConverter,
                        TChangeFeedProcessorHandler>(
                        processorName,
                        container.Container,
                        enabledFunc,
                        batchSize,
                        activationDate);
            });

        return this;
    }

    private void RegisterChangeFeedProcessor(string processorName)
    {
        if (_changeFeedProcessors.Contains(processorName))
        {
            throw new InvalidOperationException($"Change Feed Processor with the name {processorName} has already been registered");
        }

        _changeFeedProcessors.Add(processorName);
    }


    private void RegisterContainer<T>(string containerName)
    {
        var documentType = typeof(T);
        if (_containers.ContainsKey(documentType))
        {
            throw new InvalidOperationException($"Container for {documentType.Name} has already been registered");
        }

        if (_containers.Values.Contains(containerName))
        {
            throw new InvalidOperationException($"Container with the name {containerName} has already been registered.");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException("Container name cannot be empty");
        }

        _containers.Add(documentType, containerName);
    }
}
