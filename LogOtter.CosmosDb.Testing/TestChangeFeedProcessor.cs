using CosmosTestHelpers;

namespace LogOtter.CosmosDb.Testing;

public class TestChangeFeedProcessor<TRawDocument, TChangeFeedHandlerDocument>
    : IChangeFeedProcessor
{
    private readonly IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument> _changeConverter;
    private readonly IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument> _changeHandler;
    private readonly bool _enabled;

    private bool _started;

    public TestChangeFeedProcessor(
        ContainerMock container,
        IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument> changeConverter,
        IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument> changeHandler,
        bool enabled
    )
    {
        container.DocumentChanged += OnChanges;
        _changeConverter = changeConverter;
        _changeHandler = changeHandler;
        _enabled = enabled;
    }

    private async Task OnChanges(IReadOnlyCollection<object> changes, CancellationToken cancellationToken)
    {
        if (!_enabled || !_started)
        {
            return;
        }

        var convertedChanges = changes
            .Cast<TRawDocument>()
            .Select(_changeConverter.ConvertChange)
            .ToList()
            .AsReadOnly();

        await _changeHandler.ProcessChanges(convertedChanges, cancellationToken);
    }

    public Task Start()
    {
        _started = true;
        return Task.CompletedTask;
    }

    public Task Stop()
    {
        _started = false;
        return Task.CompletedTask;
    }
}