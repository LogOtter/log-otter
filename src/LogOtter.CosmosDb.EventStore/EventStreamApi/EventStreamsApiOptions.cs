namespace LogOtter.CosmosDb.EventStore.EventStreamApi;

public record EventStreamsApiOptions {
    public string RoutePrefix { get; init; } = "/logotter/api";
    public bool EnableCors { get; init; }
    public string? AccessControlAllowMethods { get; init; }
    public string? AccessControlAllowOrigin { get; init; }
    public string? AccessControlAllowCredentials { get; init; }
    public string? AccessControlAllowHeaders { get; init; }
}