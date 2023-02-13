using Microsoft.Extensions.Hosting;

namespace LogOtter.SimpleHealthChecks.Tests;

internal static class BackgroundServiceExtensions
{
    public static async Task Run(this BackgroundService service, Func<Task> action)
    {
        var stopTokenSource = new CancellationTokenSource();
        await service.StartAsync(stopTokenSource.Token);

        try
        {
            await action();
        }
        finally
        {
            stopTokenSource.Cancel();
        }
    }
}
