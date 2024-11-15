using LogOtter.CosmosDb.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.Services;

internal class StartupService : IHostedService, IHealthCheck
{
    private readonly ContainerCatalog _catalog;
    private readonly AutoProvisionSettings _autoProvisionSettings;
    private readonly ICosmosDatabaseFactory _cosmosDatabaseFactory;
    private readonly ICosmosContainerFactory _cosmosContainerFactory;
    private readonly IOptions<CosmosDbOptions> _cosmosDbOptions;
    private readonly IServiceScope _scope;
    private readonly ILogger<StartupService> _logger;

    private bool _dbCreated;
    private Exception? _dbCreationException;
    private readonly List<IChangeFeedProcessor> _changeFeedProcessors;

    public StartupService(
        ContainerCatalog catalog,
        AutoProvisionSettings autoProvisionSettings,
        ICosmosDatabaseFactory cosmosDatabaseFactory,
        ICosmosContainerFactory cosmosContainerFactory,
        IOptions<CosmosDbOptions> cosmosDbOptions,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<StartupService> logger
    )
    {
        _catalog = catalog;
        _autoProvisionSettings = autoProvisionSettings;
        _cosmosDatabaseFactory = cosmosDatabaseFactory;
        _cosmosContainerFactory = cosmosContainerFactory;
        _cosmosDbOptions = cosmosDbOptions;
        _logger = logger;

        _scope = serviceScopeFactory.CreateScope();
        _changeFeedProcessors = _scope.ServiceProvider.GetServices<IChangeFeedProcessor>().ToList();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_dbCreated)
        {
            await ProvisionDatabase(cancellationToken);
        }

        await StartChangeFeedProcessors();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_changeFeedProcessors.Select(cfp => cfp.Stop()));
    }

    private async Task StartChangeFeedProcessors()
    {
        var tasks = new List<Task>();

        foreach (var processor in _changeFeedProcessors)
        {
            _logger.LogInformation("Starting change feed processor {ProcessorName}", processor.Name);
            tasks.Add(processor.Start());
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProvisionDatabase(CancellationToken stoppingToken)
    {
        if (!_autoProvisionSettings.Enabled)
        {
            _dbCreated = true;
            _logger.LogInformation("Database AutoProvision is not enabled");
            return;
        }

        var cosmosOptions = _cosmosDbOptions.Value;

        _logger.LogInformation("Provisioning database {DatabaseId}", cosmosOptions.DatabaseId);

        try
        {
            await _cosmosDatabaseFactory.CreateDatabaseIfNotExistsAsync(
                cosmosOptions.DatabaseId,
                _autoProvisionSettings.Throughput,
                cancellationToken: stoppingToken
            );

            await _cosmosContainerFactory.CreateContainerIfNotExistsAsync(
                cosmosOptions.ChangeFeedProcessorOptions.LeasesContainerName,
                "/id",
                cancellationToken: stoppingToken
            );

            foreach (var container in _catalog.Containers)
            {
                var autoProvisionMetadata = _catalog.GetAutoProvisionSettings(container);

                await _cosmosContainerFactory.CreateContainerIfNotExistsAsync(
                    container,
                    autoProvisionMetadata.PartitionKeyPath,
                    autoProvisionMetadata.UniqueKeyPolicy,
                    autoProvisionMetadata.DefaultTimeToLive,
                    autoProvisionMetadata.IndexingPolicy,
                    autoProvisionMetadata.ThroughputProperties,
                    stoppingToken
                );
            }

            _dbCreated = true;
            _logger.LogInformation("Database AutoProvision successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to AutoProvision database");
            _dbCreated = false;
            _dbCreationException = ex;
        }
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (_dbCreated)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        if (_dbCreationException != null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Database creation failed."));
        }

        return Task.FromResult(HealthCheckResult.Degraded("Database creation is still in progress."));
    }

    public async Task ProvisionCosmosDb(CancellationToken cancellationToken)
    {
        if (_dbCreated)
        {
            throw new InvalidOperationException("Cannot provision database after it has been created");
        }

        await ProvisionDatabase(cancellationToken);
    }
}
