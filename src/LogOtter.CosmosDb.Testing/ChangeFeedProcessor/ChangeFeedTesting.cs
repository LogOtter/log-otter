using System.Diagnostics;

namespace LogOtter.CosmosDb.Testing;

public class ChangeFeedTesting
{
    public static async Task WaitFor(Func<Task> action, TimeSpan? timeout = null)
    {
        var success = false;
        Exception? ex = null;
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < (timeout ?? TimeSpan.FromSeconds(5)))
        {
            try
            {
                await action.Invoke();
                success = true;
                break;
            }
            catch (Exception thrown)
            {
                ex = thrown;
                await Task.Delay(50);
            }
        }
        if (!success)
        {
            throw ex!;
        }
    }
}
