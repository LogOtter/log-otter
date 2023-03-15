using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LogOtter.CosmosDb;

public class ChangeFeedProcessorRunnerService : IHostedService, IDisposable
{
    private readonly IList<IChangeFeedProcessor> _changeFeedProcessors;
    private readonly IServiceScope _scope;

    public ChangeFeedProcessorRunnerService(IServiceScopeFactory serviceScopeFactory)
    {
        _scope = serviceScopeFactory.CreateScope();

        _changeFeedProcessors = _scope.ServiceProvider.GetServices<IChangeFeedProcessor>().ToList();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_changeFeedProcessors.Select(cfp => cfp.Start()));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_changeFeedProcessors.Select(cfp => cfp.Stop()));
    }
}
