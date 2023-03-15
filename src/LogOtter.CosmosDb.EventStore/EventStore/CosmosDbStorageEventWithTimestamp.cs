using Newtonsoft.Json;

namespace LogOtter.CosmosDb.EventStore;

internal class CosmosDbStorageEventWithTimestamp : CosmosDbStorageEvent
{
    private static readonly DateTimeOffset EpochBase = new(
        1970,
        1,
        1,
        0,
        0,
        0,
        0,
        TimeSpan.Zero);

    [JsonProperty("_ts")]
    public int TimestampSeconds { get; set; }

    public DateTimeOffset Timestamp => EpochBase.AddSeconds(TimestampSeconds);
}
