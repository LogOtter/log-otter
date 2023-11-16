namespace CustomerWorker.Services;

public class DummyWorker(ILogger<DummyWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }
}
