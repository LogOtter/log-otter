using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace LogOtter.EventStore.CosmosDb;

internal class ChangeFeedProcessor<TRawDocument, TChangeFeedHandlerDocument, TDocument> : IChangeFeedProcessor
{
    private readonly string _name;
    private readonly Container _documentContainer;
    private readonly Container _leaseContainer;
    private readonly bool _enabled;
    private readonly int _batchSize;
    private readonly DateTime? _activationDate;
    private IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument> _feedChangeConverter;
    private readonly IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument> _changeHandler;
    private readonly ILogger<ChangeFeedProcessor<TRawDocument, TChangeFeedHandlerDocument, TDocument>> _logger;

    private readonly TimeSpan _fullBatchDelay;
    private readonly TimeSpan _errorDelay;
    private readonly string _instanceName;
    private ChangeFeedProcessor? _changeFeedProcessor;

    public ChangeFeedProcessor(
        string name,
        Container documentContainer,
        Container leaseContainer,
        bool enabled,
        int? batchSize,
        DateTime? activationDate,
        IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument> feedChangeConverter,
        IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument> changeHandler,
        ILogger<ChangeFeedProcessor<TRawDocument, TChangeFeedHandlerDocument, TDocument>> logger
    )
    {
        _name = name;
        _documentContainer = documentContainer;
        _leaseContainer = leaseContainer;
        _enabled = enabled;
        _batchSize = batchSize ?? 100;
        _activationDate = activationDate;
        _feedChangeConverter = feedChangeConverter;
        _changeHandler = changeHandler;
        _logger = logger;
        
        _fullBatchDelay = TimeSpan.FromMilliseconds(500);
        _errorDelay = TimeSpan.FromSeconds(5);
        _instanceName = Environment.MachineName;
    }

    public async Task Start()
    {
        if (!_enabled)
        {
            _logger.LogInformation(
                "Not starting change feed processor {Name} (Instance: {InstanceName}) because it is disabled",
                _name,
                _instanceName
            );
            return;
        }

        _changeFeedProcessor = _documentContainer
            .GetChangeFeedProcessorBuilder<TRawDocument>(_name, OnChanges)
            .WithErrorNotification(OnError)
            .WithInstanceName(_instanceName)
            .WithLeaseContainer(_leaseContainer)
            .WithLeaseConfiguration(acquireInterval: TimeSpan.FromMinutes(1))
            .WithStartTime(_activationDate?.ToUniversalTime() ?? DateTime.MinValue.ToUniversalTime())
            .WithMaxItems(_batchSize)
            .Build();

        await _changeFeedProcessor.StartAsync();

        _logger.LogInformation(
            "Started change feed processor {Name} (Instance: {InstanceName})", _name,
            _instanceName
        );
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
        _logger.LogError(
            exception,
            "Error for change feed processor {Name} (Instance: {InstanceName})", _name,
            _instanceName
        );

        return Task.CompletedTask;
    }

    private async Task OnChanges(IReadOnlyCollection<TRawDocument> changes, CancellationToken cancellationToken)
    {
        try
        {
            var convertedChanges = changes
                .Select(_feedChangeConverter.ConvertChange)
                .ToList()
                .AsReadOnly();
            
            await _changeHandler.ProcessChanges(convertedChanges, cancellationToken);

            _logger.LogInformation(
                "Processed batch of {BatchSize} changes for change feed processor {Name} (Instance: {InstanceName})",
                changes.Count, _name,
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