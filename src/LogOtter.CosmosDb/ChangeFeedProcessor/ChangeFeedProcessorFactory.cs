using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb;

public class ChangeFeedProcessorFactory(Database database, IOptions<CosmosDbOptions> options, IServiceScopeFactory serviceScopeFactory)
    : IChangeFeedProcessorFactory
{
    private readonly ChangeFeedProcessorOptions _options = options.Value.ChangeFeedProcessorOptions;

    public IChangeFeedProcessor CreateChangeFeedProcessor<
        TRawDocument,
        TDocument,
        TChangeFeedHandlerDocument,
        TChangeFeedChangeConverter,
        TChangeFeedProcessorHandler
    >(string processorName, Container documentContainer, Func<IServiceProvider, Task<bool>>? enabledFunc, int batchSize, DateTime? activationDate)
        where TChangeFeedProcessorHandler : IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument>
        where TChangeFeedChangeConverter : IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument>
    {
        using var scope = serviceScopeFactory.CreateScope();

        var changeConverter = scope.ServiceProvider.GetRequiredService<TChangeFeedChangeConverter>();
        var changeHandler = scope.ServiceProvider.GetRequiredService<TChangeFeedProcessorHandler>();

        var enabled = true;
        if (enabledFunc != null)
        {
            enabled = enabledFunc(scope.ServiceProvider).GetAwaiter().GetResult();
        }

        var autoProvisionSettings = scope.ServiceProvider.GetRequiredService<AutoProvisionSettings>();

        var leaseContainer = autoProvisionSettings.Enabled
            ? database.CreateContainerIfNotExistsAsync(_options.LeasesContainerName, "/id").GetAwaiter().GetResult().Container
            : database.GetContainer(_options.LeasesContainerName);

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ChangeFeedProcessor<TRawDocument, TChangeFeedHandlerDocument>>>();

        return new ChangeFeedProcessor<TRawDocument, TChangeFeedHandlerDocument>(
            processorName,
            documentContainer,
            leaseContainer,
            enabled,
            batchSize,
            activationDate,
            _options,
            changeConverter,
            changeHandler,
            logger
        );
    }
}
