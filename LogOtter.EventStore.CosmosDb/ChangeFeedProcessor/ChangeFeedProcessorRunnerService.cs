using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LogOtter.EventStore.CosmosDb;

public class ChangeFeedProcessorRunnerService : IHostedService, IDisposable
{
    private readonly IServiceScope _scope;
    private readonly IList<IChangeFeedProcessor> _changeFeedProcessors;

    public ChangeFeedProcessorRunnerService(IServiceScopeFactory serviceScopeFactory)
    {
        _scope = serviceScopeFactory.CreateScope();
        
        _changeFeedProcessors = _scope
            .ServiceProvider
            .GetServices<IChangeFeedProcessor>()
            .ToList();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_changeFeedProcessors.Select(cfp => cfp.Start()));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_changeFeedProcessors.Select(cfp => cfp.Stop()));
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}