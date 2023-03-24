using System.Reflection;
using LogOtter.CosmosDb.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogOtter.CosmosDb;

public class CosmosDbBuilder
{
    private static readonly MethodInfo RegisterCosmosContainerMethod = typeof(CosmosDbBuilder).GetMethod(
        nameof(RegisterCosmosContainer),
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    private static readonly MethodInfo AddChangeFeedProcessorMethod = typeof(CosmosDbBuilder).GetMethod(
        nameof(AddChangeFeedProcessorInternal),
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    private readonly IList<string> _changeFeedProcessors;
    private readonly IDictionary<Type, string> _containers;

    public CosmosDbBuilder(IServiceCollection serviceCollection)
    {
        _containers = new Dictionary<Type, string>();
        _changeFeedProcessors = new List<string>();

        Services = serviceCollection;
    }

    internal IServiceCollection Services { get; }

    public CosmosDbBuilder WithAutoProvisioning()
    {
        Services.RemoveAll<AutoProvisionSettings>();
        Services.AddSingleton(_ => new AutoProvisionSettings(true));
        return this;
    }

    public CosmosDbBuilder AddContainer<T>(string containerName, Action<ContainerConfiguration>? configure = null)
    {
        var config = new ContainerConfiguration();
        configure?.Invoke(config);

        return AddContainer(typeof(T), containerName, config.AutoProvisionMetadata);
    }

    internal CosmosDbBuilder AddContainer(Type documentType, string containerName, AutoProvisionMetadata? autoProvisionMetadata)
    {
        RegisterContainer(documentType, containerName);

        var genericMethod = RegisterCosmosContainerMethod.MakeGenericMethod(documentType);
        genericMethod.Invoke(this, new object?[] { containerName, autoProvisionMetadata });

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
        DateTime? activationDate = null
    )
    {
        var genericMethod = AddChangeFeedProcessorMethod.MakeGenericMethod(
            rawDocumentType,
            documentType,
            changeFeedHandlerDocumentType,
            changeFeedChangeConverterType,
            changeFeedProcessorHandlerType
        );
        genericMethod.Invoke(this, new object?[] { processorName, enabledFunc, batchSize, activationDate });
        return this;
    }

    private CosmosDbBuilder AddChangeFeedProcessorInternal<
        TRawDocument,
        TDocument,
        TChangeFeedHandlerDocument,
        TChangeFeedChangeConverter,
        TChangeFeedProcessorHandler
    >(string processorName, Func<IServiceProvider, Task<bool>>? enabledFunc = null, int batchSize = 100, DateTime? activationDate = null)
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

        Services.AddTransient(sp =>
        {
            var container = sp.GetRequiredService<CosmosContainer<TDocument>>();
            var changeFeedProcessorFactory = sp.GetRequiredService<IChangeFeedProcessorFactory>();

            return changeFeedProcessorFactory.CreateChangeFeedProcessor<
                TRawDocument,
                TDocument,
                TChangeFeedHandlerDocument,
                TChangeFeedChangeConverter,
                TChangeFeedProcessorHandler
            >(processorName, container.Container, enabledFunc, batchSize, activationDate);
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

    private void RegisterCosmosContainer<T>(string containerName, AutoProvisionMetadata? autoProvisionMetadata)
    {
        autoProvisionMetadata ??= new AutoProvisionMetadata();

        Services.AddSingleton(sp =>
        {
            var cosmosContainerFactory = sp.GetRequiredService<ICosmosContainerFactory>();
            var autoProvisionSettings = sp.GetRequiredService<AutoProvisionSettings>();

            var container = autoProvisionSettings.Enabled
                ? cosmosContainerFactory
                    .CreateContainerIfNotExistsAsync(
                        containerName,
                        autoProvisionMetadata.PartitionKeyPath,
                        autoProvisionMetadata.UniqueKeyPolicy,
                        autoProvisionMetadata.DefaultTimeToLive,
                        autoProvisionMetadata.CompositeIndexes,
                        autoProvisionMetadata.ThroughputProperties
                    )
                    .GetAwaiter()
                    .GetResult()
                : cosmosContainerFactory.GetContainer(containerName);

            return new CosmosContainer<T>(container);
        });
    }
}
