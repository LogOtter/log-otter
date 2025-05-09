﻿using System.Reflection;
using LogOtter.CosmosDb.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LogOtter.CosmosDb;

public class CosmosDbBuilder(IServiceCollection serviceCollection)
{
    private static readonly MethodInfo RegisterCosmosContainerMethod = typeof(CosmosDbBuilder).GetMethod(
        nameof(RegisterCosmosContainer),
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    private static readonly MethodInfo RegisterSubTypeContainerMethod = typeof(CosmosDbBuilder).GetMethod(
        nameof(RegisterSubTypeContainer),
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    private static readonly MethodInfo AddChangeFeedProcessorMethod = typeof(CosmosDbBuilder).GetMethod(
        nameof(AddChangeFeedProcessorInternal),
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    private readonly IList<string> _changeFeedProcessors = new List<string>();
    private readonly IDictionary<Type, string> _containers = new Dictionary<Type, string>();

    internal IServiceCollection Services { get; } = serviceCollection;

    public CosmosDbBuilder WithAutoProvisioning(int? databaseThroughput = null)
    {
        Services.RemoveAll<AutoProvisionSettings>();
        Services.AddSingleton(_ => new AutoProvisionSettings(true, databaseThroughput));
        return this;
    }

    public CosmosDbBuilder AddContainer<T>(string containerName, Action<ContainerConfiguration<T>>? configure = null)
    {
        var config = new ContainerConfiguration<T>();
        configure?.Invoke(config);

        return AddContainer(typeof(T), containerName, config.AutoProvisionMetadata, config.ChangeFeedProcessorsMetadata, config.SubTypes);
    }

    internal CosmosDbBuilder AddContainer(
        Type documentType,
        string containerName,
        AutoProvisionMetadata? autoProvisionMetadata,
        List<ChangeFeedProcessorMetadata> changeFeedProcessorsMetadata
    )
    {
        return AddContainer(documentType, containerName, autoProvisionMetadata, changeFeedProcessorsMetadata, new HashSet<Type>());
    }

    internal CosmosDbBuilder AddContainer(
        Type documentType,
        string containerName,
        AutoProvisionMetadata? autoProvisionMetadata,
        List<ChangeFeedProcessorMetadata> changeFeedProcessorsMetadata,
        HashSet<Type> subTypes
    )
    {
        RegisterContainer(documentType, containerName);

        var genericRegisterMethod = RegisterCosmosContainerMethod.MakeGenericMethod(documentType);
        genericRegisterMethod.Invoke(this, new object?[] { containerName, autoProvisionMetadata });

        foreach (var subType in subTypes)
        {
            var genericRegisterSubTypeMethod = RegisterSubTypeContainerMethod.MakeGenericMethod(documentType, subType);
            genericRegisterSubTypeMethod.Invoke(this, null);
        }

        foreach (var metadata in changeFeedProcessorsMetadata)
        {
            AddChangeFeedProcessor(
                metadata.RawDocumentType,
                metadata.DocumentType,
                metadata.ChangeFeedHandlerDocumentType,
                metadata.ChangeFeedChangeConverterType,
                metadata.ChangeFeedProcessorHandlerType,
                metadata.ProcessorName,
                metadata.EnabledFunc,
                metadata.BatchSize,
                metadata.ActivationDate
            );
        }

        return this;
    }

    private CosmosDbBuilder AddChangeFeedProcessor(
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
        RegisterChangeFeedProcessor(processorName);

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

    private void AddChangeFeedProcessorInternal<
        TRawDocument,
        TDocument,
        TChangeFeedHandlerDocument,
        TChangeFeedChangeConverter,
        TChangeFeedProcessorHandler
    >(string processorName, Func<IServiceProvider, Task<bool>>? enabledFunc = null, int batchSize = 100, DateTime? activationDate = null)
        where TChangeFeedChangeConverter : class, IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument>
        where TChangeFeedProcessorHandler : class, IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument>
    {
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
        Services.AddSingleton(new ContainerAutoProvisionMetadata(containerName, autoProvisionMetadata ?? new AutoProvisionMetadata()));

        Services.AddSingleton(sp =>
        {
            var cosmosContainerFactory = sp.GetRequiredService<ICosmosContainerFactory>();

            var container = cosmosContainerFactory.GetContainer(containerName);

            return new CosmosContainer<T>(container);
        });
    }

    private void RegisterSubTypeContainer<TBaseType, TSubType>()
    {
        Services.AddSingleton(sp =>
        {
            var baseTypeContainer = sp.GetRequiredService<CosmosContainer<TBaseType>>();
            return new CosmosContainer<TSubType>(baseTypeContainer.Container);
        });
    }
}
