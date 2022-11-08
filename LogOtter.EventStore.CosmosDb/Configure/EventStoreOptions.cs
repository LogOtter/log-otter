namespace LogOtter.EventStore.CosmosDb;

public class EventStoreOptions
{
    public string LeasesContainerName { get; set; } = "Leases";
}