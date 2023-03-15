namespace CustomerWorker.Services;

public class DummyWorker : BackgroundService
{
    private readonly ILogger<DummyWorker> _logger;

    public DummyWorker(ILogger<DummyWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested);
    }
}
