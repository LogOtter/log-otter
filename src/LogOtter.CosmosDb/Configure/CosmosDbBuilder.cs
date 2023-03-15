using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb;

public class CosmosDbBuilder
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

    public CosmosDbBuilder AddContainer<T>(
        string containerName,
        string partitionKeyPath = "/partitionKey",
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = null,
        IEnumerable<Collection<CompositePath>>? compositeIndexes = null,
        ThroughputProperties? throughputProperties = null) =>
        AddContainer(
            typeof(T),
            containerName,
            partitionKeyPath,
            uniqueKeyPolicy,
            defaultTimeToLive,
            compositeIndexes,
            throughputProperties);

    internal CosmosDbBuilder AddContainer(
        Type documentType,
        string containerName,
        string partitionKeyPath = "/partitionKey",
        UniqueKeyPolicy? uniqueKeyPolicy = null,
        int? defaultTimeToLive = null,
        IEnumerable<Collection<CompositePath>>? compositeIndexes = null,
        ThroughputProperties? throughputProperties = null)
    {
        RegisterContainer(documentType, containerName);

        var cosmosContainer = typeof(CosmosContainer<>);
        var registerCosmosContainer = typeof(CosmosDbBuilder).GetMethod(
            nameof(RegisterCosmosContainer),
            BindingFlags.Instance | BindingFlags.NonPublic);
        var genericMethod = registerCosmosContainer!.MakeGenericMethod(documentType);
        genericMethod.Invoke(
            this,
            new object?[] {containerName, partitionKeyPath, uniqueKeyPolicy, defaultTimeToLive, compositeIndexes, throughputProperties});

        return this;
    }

    internal CosmosDbBuilder AddChangeFeedProcessor(
        Type rawDocumentType,
        Type documentType,
        Type changeFeedHandlerDocumentType,
        Type changeFeedChangeConverterType,
        Type changeFeedProcessorHandlerType,
        string processorName,
        Func<IServiceProvider, Task<bool>>? enabledFunc = null,
        int batchSize = 100,
        DateTime? activationDate = null)
    {
        var addChangeFeedProcessor = typeof(CosmosDbBuilder).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                                                            .Single(m => m.Name == nameof(AddChangeFeedProcessorInternal));
        var genericMethod = addChangeFeedProcessor.MakeGenericMethod(
            rawDocumentType,
            documentType,
            changeFeedHandlerDocumentType,
            changeFeedChangeConverterType,
            changeFeedProcessorHandlerType);
        genericMethod.Invoke(this, new object?[] {processorName, enabledFunc, batchSize, activationDate});
        return this;
    }

    private CosmosDbBuilder
        AddChangeFeedProcessorInternal<TRawDocument, TDocument, TChangeFeedHandlerDocument, TChangeFeedChangeConverter, TChangeFeedProcessorHandler>(
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


    private void RegisterContainer(Type documentType, string containerName)
    {
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

    private void RegisterCosmosContainer<T>(
        string containerName,
        string partitionKeyPath,
        UniqueKeyPolicy? uniqueKeyPolicy,
        int? defaultTimeToLive,
        IEnumerable<Collection<CompositePath>>? compositeIndexes,
        ThroughputProperties? throughputProperties)
    {
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
    }
}
