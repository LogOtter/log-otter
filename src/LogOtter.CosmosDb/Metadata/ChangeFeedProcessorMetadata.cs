namespace LogOtter.CosmosDb.Metadata;

internal record ChangeFeedProcessorMetadata(
    Type RawDocumentType,
    Type DocumentType,
    Type ChangeFeedHandlerDocumentType,
    Type ChangeFeedChangeConverterType,
    Type ChangeFeedProcessorHandlerType,
    string ProcessorName,
    Func<IServiceProvider, Task<bool>>? EnabledFunc = null,
    int BatchSize = 100,
    DateTime? ActivationDate = null
);
