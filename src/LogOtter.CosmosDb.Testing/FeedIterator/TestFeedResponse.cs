﻿using System.Net;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Testing;

public class TestFeedResponse<T> : FeedResponse<T>
{
    public TestFeedResponse(IEnumerable<T> results)
    {
        Resource = results;
    }

    public override Headers Headers { get; } = default!;
    public override IEnumerable<T> Resource { get; }
    public override HttpStatusCode StatusCode => HttpStatusCode.OK;
    public override CosmosDiagnostics Diagnostics { get; } = default!;
    public override IEnumerator<T> GetEnumerator() => Resource.GetEnumerator();
    public override string ContinuationToken { get; } = default!;
    public override int Count => Resource.Count();
    public override string IndexMetrics { get; } = default!;
}