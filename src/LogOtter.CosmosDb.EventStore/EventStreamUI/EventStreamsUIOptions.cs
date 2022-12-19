namespace LogOtter.CosmosDb.EventStore.EventStreamUI;

public record EventStreamsUIOptions
{
    public string RoutePrefix { get; init; } = "/logotter";
}