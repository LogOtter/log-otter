using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LogOtter.CosmosDb.Tests;

public class ChangeFeedProcessorTests
{
    private const int BatchSize = 100;

    private static readonly TimeSpan DrainTime = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    private readonly TestLogger<ChangeFeedProcessor<int, int>> _logger = new();

    [Fact]
    public async Task StoppingPartWayThroughABatchCancelsItWithoutADrainTime()
    {
        var handler = new TestChangeHandler(pauseAfterFirstChange: true);
        var processor = CreateProcessor(handler, new ChangeFeedProcessorOptions { GracefulShutdownDrainTime = TimeSpan.Zero });
        using var sdkCancellation = new CancellationTokenSource();

        var processing = processor.OnChanges(CreateBatch(), sdkCancellation.Token);
        await StopPartWayThroughBatch(processor, handler, sdkCancellation);

        await Should.ThrowAsync<TaskCanceledException>(() => processing.WaitAsync(TestTimeout, TestContext.Current.CancellationToken));
        handler.ProcessedCount.ShouldBe(1);
    }

    [Fact]
    public async Task StoppingDuringTheFullBatchDelayCancelsItWithoutADrainTime()
    {
        var handler = new TestChangeHandler();
        var processor = CreateProcessor(
            handler,
            new ChangeFeedProcessorOptions { FullBatchDelay = TimeSpan.FromSeconds(30), GracefulShutdownDrainTime = TimeSpan.Zero }
        );
        using var sdkCancellation = new CancellationTokenSource();

        var processing = processor.OnChanges(CreateBatch(), sdkCancellation.Token);
        await StopDuringFullBatchDelay(processor, sdkCancellation);

        await Should.ThrowAsync<TaskCanceledException>(() => processing.WaitAsync(TestTimeout, TestContext.Current.CancellationToken));
        handler.ProcessedCount.ShouldBe(BatchSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public async Task ABatchThatIsNotStoppedIsProcessedInFull(int drainTimeSeconds)
    {
        var handler = new TestChangeHandler();
        var processor = CreateProcessor(
            handler,
            new ChangeFeedProcessorOptions
            {
                GracefulShutdownDrainTime = TimeSpan.FromSeconds(drainTimeSeconds),
                FullBatchDelay = TimeSpan.FromMilliseconds(10),
            }
        );
        using var sdkCancellation = new CancellationTokenSource();

        await processor.OnChanges(CreateBatch(), sdkCancellation.Token).WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

        handler.ProcessedCount.ShouldBe(BatchSize);
        _logger.Entries.ShouldContain(e =>
            e.Message.StartsWith($"Processed batch of {BatchSize} changes") && !e.Message.EndsWith("while shutting down")
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public async Task AFailingBatchWaitsForTheErrorDelayBeforeRethrowing(int drainTimeSeconds)
    {
        var errorDelay = TimeSpan.FromMilliseconds(200);
        var handler = new TestChangeHandler(throwOnChange: 0);
        var processor = CreateProcessor(
            handler,
            new ChangeFeedProcessorOptions { GracefulShutdownDrainTime = TimeSpan.FromSeconds(drainTimeSeconds), ErrorDelay = errorDelay }
        );
        using var sdkCancellation = new CancellationTokenSource();
        var stopwatch = Stopwatch.StartNew();

        await Should.ThrowAsync<InvalidOperationException>(() =>
            processor.OnChanges(CreateBatch(), sdkCancellation.Token).WaitAsync(TestTimeout, TestContext.Current.CancellationToken)
        );

        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(errorDelay * 0.75);
    }

    [Fact]
    public async Task StoppingPartWayThroughABatchLetsItFinishWithADrainTime()
    {
        var handler = new TestChangeHandler(pauseAfterFirstChange: true);
        var processor = CreateProcessor(handler, new ChangeFeedProcessorOptions { GracefulShutdownDrainTime = DrainTime });
        using var sdkCancellation = new CancellationTokenSource();

        var processing = processor.OnChanges(CreateBatch(), sdkCancellation.Token);
        await StopPartWayThroughBatch(processor, handler, sdkCancellation);

        await processing.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        handler.ProcessedCount.ShouldBe(BatchSize);
        _logger.Entries.ShouldContain(e =>
            e.Message.StartsWith($"Processed batch of {BatchSize} changes") && e.Message.EndsWith("while shutting down")
        );
    }

    [Fact]
    public async Task StoppingDuringTheFullBatchDelayReturnsWithADrainTime()
    {
        var handler = new TestChangeHandler();
        var processor = CreateProcessor(
            handler,
            new ChangeFeedProcessorOptions { GracefulShutdownDrainTime = DrainTime, FullBatchDelay = TimeSpan.FromSeconds(30) }
        );
        using var sdkCancellation = new CancellationTokenSource();

        var processing = processor.OnChanges(CreateBatch(), sdkCancellation.Token);
        await StopDuringFullBatchDelay(processor, sdkCancellation);

        await processing.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        handler.ProcessedCount.ShouldBe(BatchSize);
    }

    [Fact]
    public async Task LosingTheLeasePartWayThroughABatchCancelsItEvenWithADrainTime()
    {
        var handler = new TestChangeHandler(pauseAfterFirstChange: true);
        var processor = CreateProcessor(handler, new ChangeFeedProcessorOptions { GracefulShutdownDrainTime = DrainTime });
        using var sdkCancellation = new CancellationTokenSource();

        var processing = processor.OnChanges(CreateBatch(), sdkCancellation.Token);

        await handler.FirstChangeProcessed.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        await sdkCancellation.CancelAsync();
        handler.Resume();

        await Should.ThrowAsync<TaskCanceledException>(() => processing.WaitAsync(TestTimeout, TestContext.Current.CancellationToken));
        handler.ProcessedCount.ShouldBe(1);
        _logger.Entries.ShouldNotContain(e => e.Message.EndsWith("while shutting down"));
    }

    [Fact]
    public async Task AFailureWhileDrainingIsRethrownWithoutTheErrorDelay()
    {
        var handler = new TestChangeHandler(pauseAfterFirstChange: true, throwOnChange: 1);
        var processor = CreateProcessor(
            handler,
            new ChangeFeedProcessorOptions { GracefulShutdownDrainTime = DrainTime, ErrorDelay = TimeSpan.FromSeconds(30) }
        );
        using var sdkCancellation = new CancellationTokenSource();

        var processing = processor.OnChanges(CreateBatch(), sdkCancellation.Token);
        await StopPartWayThroughBatch(processor, handler, sdkCancellation);

        await Should.ThrowAsync<InvalidOperationException>(() => processing.WaitAsync(TestTimeout, TestContext.Current.CancellationToken));
        handler.ProcessedCount.ShouldBe(1);
    }

    [Fact]
    public async Task ABatchThatDoesNotFinishWithinTheDrainTimeIsCancelled()
    {
        var handler = new TestChangeHandler(pauseAfterFirstChange: true, blockAfterFirstChange: true);
        var processor = CreateProcessor(
            handler,
            new ChangeFeedProcessorOptions { GracefulShutdownDrainTime = TimeSpan.FromMilliseconds(100), ErrorDelay = TimeSpan.FromSeconds(30) }
        );
        using var sdkCancellation = new CancellationTokenSource();

        var processing = processor.OnChanges(CreateBatch(), sdkCancellation.Token);
        await StopPartWayThroughBatch(processor, handler, sdkCancellation);

        await Should.ThrowAsync<TaskCanceledException>(() => processing.WaitAsync(TestTimeout, TestContext.Current.CancellationToken));
        handler.ProcessedCount.ShouldBe(1);
        _logger.Entries.ShouldContain(e =>
            e.Level == LogLevel.Warning && e.Message.Contains("didn't finish within the graceful shutdown drain time")
        );
    }

    private ChangeFeedProcessor<int, int> CreateProcessor(TestChangeHandler handler, ChangeFeedProcessorOptions options) =>
        new("test-processor", null!, null!, true, BatchSize, null, options, new NoChangeFeedChangeConverter<int>(), handler, _logger);

    private static IReadOnlyCollection<int> CreateBatch() => Enumerable.Range(0, BatchSize).ToList().AsReadOnly();

    private static async Task StopPartWayThroughBatch(
        ChangeFeedProcessor<int, int> processor,
        TestChangeHandler handler,
        CancellationTokenSource sdkCancellation
    )
    {
        await handler.FirstChangeProcessed.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        await processor.Stop();
        await sdkCancellation.CancelAsync();
        handler.Resume();
    }

    private async Task StopDuringFullBatchDelay(ChangeFeedProcessor<int, int> processor, CancellationTokenSource sdkCancellation)
    {
        var batchProcessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _logger.Logged += entry =>
        {
            if (entry.Message.StartsWith("Processed batch"))
            {
                batchProcessed.TrySetResult();
            }
        };

        if (_logger.Entries.Any(e => e.Message.StartsWith("Processed batch")))
        {
            batchProcessed.TrySetResult();
        }

        await batchProcessed.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        await processor.Stop();
        await sdkCancellation.CancelAsync();
    }
}
