namespace LogOtter.CosmosDb.EventStore.EventStreamApi;

public class EventStreamsApiOptions
{
    public string RoutePrefix { get; set; } = "/event-streams/api";
    public bool EnableCors { get; set; }
    public string? AccessControlAllowMethods { get; set; }
    public string? AccessControlAllowOrigin { get; set; }
    public string? AccessControlAllowCredentials { get; set; }
    public string? AccessControlAllowHeaders { get; set; }
}