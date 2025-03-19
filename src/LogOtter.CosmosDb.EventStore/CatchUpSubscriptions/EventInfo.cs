namespace LogOtter.CosmosDb.EventStore;

public record EventInfo(DateTimeOffset CreatedOn, int EventNumber, Dictionary<string, string> Metadata);
