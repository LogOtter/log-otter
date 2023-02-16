using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi;

public record EventStreamsApiOptions {
    public PathString RoutePrefix { get; set; } = "/logotter/api";
    public bool EnableCors { get; set; }
    public string? AccessControlAllowMethods { get; set; }
    public string? AccessControlAllowOrigin { get; set; }
    public string? AccessControlAllowCredentials { get; set; }
    public string? AccessControlAllowHeaders { get; set; }
}
