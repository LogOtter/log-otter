using LogOtter.CosmosDb.ContainerMock.ContainerMockData;

namespace LogOtter.CosmosDb.Testing;

public class TestChangeFeedProcessor<TRawDocument, TChangeFeedHandlerDocument> : IChangeFeedProcessor
{
    private readonly IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument> _changeConverter;
    private readonly IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument> _changeHandler;
    private readonly bool _enabled;

    private bool _started;

    public TestChangeFeedProcessor(
        ContainerMock.ContainerMock container,
        IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument> changeConverter,
        IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument> changeHandler,
        bool enabled)
    {
        container.DataChanged += OnChanges;
        _changeConverter = changeConverter;
        _changeHandler = changeHandler;
        _enabled = enabled;
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

    private void OnChanges(object? sender, DataChangedEventArgs e)
    {
        if (!_enabled || !_started)
        {
            return;
        }

        var changes = new List<TRawDocument> { e.Deserialize<TRawDocument>() };

        var convertedChanges = changes.Select(_changeConverter.ConvertChange).ToList().AsReadOnly();

        _changeHandler.ProcessChanges(convertedChanges, CancellationToken.None).GetAwaiter().GetResult();
    }
}
