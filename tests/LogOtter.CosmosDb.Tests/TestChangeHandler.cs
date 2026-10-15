namespace LogOtter.CosmosDb.Tests;

// Processes each change with a short delay that honours the cancellation token, like a real handler doing I/O. Can pause
// after the first change until resumed, so a test can stop the processor part way through a batch.
public class TestChangeHandler(bool pauseAfterFirstChange = false, int? throwOnChange = null, bool blockAfterFirstChange = false)
    : IChangeFeedProcessorChangeHandler<int>
{
    private readonly TaskCompletionSource _firstChangeProcessed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _processedCount;

    public int ProcessedCount => _processedCount;

    public Task FirstChangeProcessed => _firstChangeProcessed.Task;

    public void Resume() => _resume.TrySetResult();

    public async Task ProcessChanges(IReadOnlyCollection<int> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            if (change == throwOnChange)
            {
                throw new InvalidOperationException($"Failed to process change {change}");
            }

            await Task.Delay(1, cancellationToken);
            Interlocked.Increment(ref _processedCount);

            if (change == 0 && pauseAfterFirstChange)
            {
                _firstChangeProcessed.TrySetResult();
                await _resume.Task;

                if (blockAfterFirstChange)
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
            }
        }
    }
}
