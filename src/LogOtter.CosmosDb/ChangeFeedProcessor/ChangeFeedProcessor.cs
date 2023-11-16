using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace LogOtter.CosmosDb;

internal class ChangeFeedProcessor<TRawDocument, TChangeFeedHandlerDocument>(
    string name,
    Container documentContainer,
    Container leaseContainer,
    bool enabled,
    int? batchSize,
    DateTime? activationDate,
    ChangeFeedProcessorOptions options,
    IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument> feedChangeConverter,
    IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument> changeHandler,
    ILogger<ChangeFeedProcessor<TRawDocument, TChangeFeedHandlerDocument>> logger
) : IChangeFeedProcessor
{
    private readonly int _batchSize = batchSize ?? options.DefaultBatchSize;
    private readonly TimeSpan _errorDelay = options.ErrorDelay;

    private readonly TimeSpan _fullBatchDelay = options.FullBatchDelay;
    private readonly string _instanceName = Environment.MachineName;
    private ChangeFeedProcessor? _changeFeedProcessor;

    public async Task Start()
    {
        if (!enabled)
        {
            logger.LogInformation("Not starting change feed processor {Name} (Instance: {InstanceName}) because it is disabled", name, _instanceName);
            return;
        }

        _changeFeedProcessor = documentContainer
            .GetChangeFeedProcessorBuilder<TRawDocument>(name, OnChanges)
            .WithErrorNotification(OnError)
            .WithInstanceName(_instanceName)
            .WithLeaseContainer(leaseContainer)
            .WithLeaseConfiguration(TimeSpan.FromMinutes(1))
            .WithStartTime(activationDate?.ToUniversalTime() ?? DateTime.MinValue.ToUniversalTime())
            .WithMaxItems(_batchSize)
            .Build();

        await _changeFeedProcessor.StartAsync();

        logger.LogInformation("Started change feed processor {Name} (Instance: {InstanceName})", name, _instanceName);
    }

    public async Task Stop()
    {
        if (_changeFeedProcessor == null)
        {
            return;
        }

        await _changeFeedProcessor.StopAsync();
    }

    private Task OnError(string leaseToken, Exception exception)
    {
        logger.LogError(exception, "Error for change feed processor {Name} (Instance: {InstanceName})", name, _instanceName);

        return Task.CompletedTask;
    }

    private async Task OnChanges(IReadOnlyCollection<TRawDocument> changes, CancellationToken cancellationToken)
    {
        try
        {
            var convertedChanges = changes.Select(feedChangeConverter.ConvertChange).ToList().AsReadOnly();

            await changeHandler.ProcessChanges(convertedChanges, cancellationToken);

            logger.LogInformation(
                "Processed batch of {BatchSize} changes for change feed processor {Name} (Instance: {InstanceName})",
                changes.Count,
                name,
                _instanceName
            );

            if (changes.Count == _batchSize)
            {
                // Intentional delay to avoid a CFP that is catching up on a large container from consuming too many resources
                await Task.Delay(_fullBatchDelay, cancellationToken);
            }
        }
        catch (Exception)
        {
            // Wait a bit as immediately trying will consume too many resources
            await Task.Delay(_errorDelay, cancellationToken);
            throw;
        }
    }
}
