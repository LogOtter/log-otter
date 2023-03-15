namespace LogOtter.CosmosDb;

public class ChangeFeedProcessorOptions
{
    public string LeasesContainerName { get; set; } = "Leases";
    public int DefaultBatchSize { get; set; } = 100;
    public TimeSpan FullBatchDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan ErrorDelay { get; set; } = TimeSpan.FromSeconds(5);
}
