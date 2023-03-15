using Microsoft.AspNetCore.Http;

namespace LogOtter.CosmosDb.EventStore.EventStreamApi;

public record EventStreamsApiOptions
{
    public PathString RoutePrefix { get; set; } = "/logotter/api";
}
