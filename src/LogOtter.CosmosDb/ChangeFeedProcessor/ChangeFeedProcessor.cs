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
    private readonly TimeSpan _gracefulShutdownDrainTime = options.GracefulShutdownDrainTime;
    private readonly string _instanceName = options.InstanceName;
    private ChangeFeedProcessor? _changeFeedProcessor;
    private volatile bool _stopping;

    public string Name => name;

    // Stop has been called and a drain time is configured, so an in-flight batch should be finished rather than abandoned
    private bool DrainingForShutdown => _stopping && _gracefulShutdownDrainTime > TimeSpan.Zero;

    public async Task Start()
    {
        _stopping = false;

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
        _stopping = true;

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

    internal async Task OnChanges(IReadOnlyCollection<TRawDocument> changes, CancellationToken cancellationToken)
    {
        // The SDK cancels cancellationToken as soon as the processor is stopped (e.g. during deploy), abandoning the batch
        // which then isn't checkpointed so the next lease owner processes it again. With a drain time the batch gets its own
        // token, which on a graceful shutdown is cancelled once the drain time has passed, so the in flight batch can
        // finish and be checkpointed
        using var drainSource = _gracefulShutdownDrainTime > TimeSpan.Zero ? new CancellationTokenSource() : null;
        await using var cancellationRegistration = drainSource == null ? default : cancellationToken.Register(OnBatchCancelled, drainSource);

        try
        {
            var convertedChanges = changes.Select(feedChangeConverter.ConvertChange).ToList().AsReadOnly();

            await changeHandler.ProcessChanges(convertedChanges, drainSource?.Token ?? cancellationToken);
        }
        catch (OperationCanceledException) when (DrainingForShutdown && drainSource is { IsCancellationRequested: true })
        {
            logger.LogWarning(
                "Batch of {BatchSize} changes for change feed processor {Name} (Instance: {InstanceName}) didn't finish within the graceful shutdown drain time of {DrainTime}, so will be processed again",
                changes.Count,
                name,
                _instanceName,
                _gracefulShutdownDrainTime
            );
            throw;
        }
        catch (Exception) when (!DrainingForShutdown)
        {
            // Wait a bit as immediately trying will consume too many resources
            await Task.Delay(_errorDelay, cancellationToken);
            throw;
        }

        // No need to delay when shutting down
        if (DrainingForShutdown)
        {
            logger.LogInformation(
                "Processed batch of {BatchSize} changes for change feed processor {Name} (Instance: {InstanceName}) while shutting down",
                changes.Count,
                name,
                _instanceName
            );
            return;
        }

        logger.LogInformation(
            "Processed batch of {BatchSize} changes for change feed processor {Name} (Instance: {InstanceName})",
            changes.Count,
            name,
            _instanceName
        );

        if (changes.Count == _batchSize)
        {
            try
            {
                // Intentional delay to avoid a CFP that is catching up on a large container from consuming too many resources
                await Task.Delay(_fullBatchDelay, cancellationToken);
            }
            catch (OperationCanceledException) when (DrainingForShutdown)
            {
                // Stopped during the delay, but we finished already so return normally to have it checkpointed
            }
        }
    }

    private void OnBatchCancelled(object? drainSource)
    {
        var source = (CancellationTokenSource)drainSource!;

        // Anything other than a graceful shutdown (e.g. the lease being lost to another instance) cancels the batch
        // straight away, as it would without a drain time
        if (_stopping)
        {
            source.CancelAfter(_gracefulShutdownDrainTime);
        }
        else
        {
            source.Cancel();
        }
    }
}
