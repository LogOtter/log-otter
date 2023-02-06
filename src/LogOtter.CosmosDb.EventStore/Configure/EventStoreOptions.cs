namespace LogOtter.CosmosDb.EventStore;

public record EventStoreOptions
{
    public bool AutoEscapeIds { get; set; }
}