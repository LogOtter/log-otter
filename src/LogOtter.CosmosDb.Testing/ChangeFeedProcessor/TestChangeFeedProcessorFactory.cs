using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.Testing;

public class TestChangeFeedProcessorFactory : IChangeFeedProcessorFactory
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TestChangeFeedProcessorFactory(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public IChangeFeedProcessor
        CreateChangeFeedProcessor<TRawDocument, TDocument, TChangeFeedHandlerDocument, TChangeFeedChangeConverter, TChangeFeedProcessorHandler>(
            string processorName,
            Container documentContainer,
            Func<IServiceProvider, Task<bool>>? enabledFunc,
            int batchSize,
            DateTime? activationDate)
        where TChangeFeedChangeConverter : IChangeFeedChangeConverter<TRawDocument, TChangeFeedHandlerDocument>
        where TChangeFeedProcessorHandler : IChangeFeedProcessorChangeHandler<TChangeFeedHandlerDocument>
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var container = scope.ServiceProvider.GetRequiredService<CosmosContainer<TDocument>>();
        var changeConverter = scope.ServiceProvider.GetRequiredService<TChangeFeedChangeConverter>();
        var changeHandler = scope.ServiceProvider.GetRequiredService<TChangeFeedProcessorHandler>();

        var enabled = true;
        if (enabledFunc != null)
        {
            enabled = enabledFunc(scope.ServiceProvider).GetAwaiter().GetResult();
        }

        return new TestChangeFeedProcessor<TRawDocument, TChangeFeedHandlerDocument>(
            (ContainerMock.ContainerMock)container.Container,
            changeConverter,
            changeHandler,
            enabled);
    }
}
