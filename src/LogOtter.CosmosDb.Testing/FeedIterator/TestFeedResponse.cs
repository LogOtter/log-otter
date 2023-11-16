using System.Net;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Testing;

public class TestFeedResponse<T>(IEnumerable<T> results) : FeedResponse<T>
{
    public override Headers Headers { get; } = default!;
    public override IEnumerable<T> Resource { get; } = results;
    public override HttpStatusCode StatusCode => HttpStatusCode.OK;
    public override CosmosDiagnostics Diagnostics { get; } = default!;
    public override string ContinuationToken { get; } = default!;
    public override int Count => Resource.Count();
    public override string IndexMetrics { get; } = default!;

    public override IEnumerator<T> GetEnumerator()
    {
        return Resource.GetEnumerator();
    }
}
