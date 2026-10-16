using System.Diagnostics;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class ActivityMetadataEnricherTests
{
    private readonly ActivityMetadataEnricher _enricher = new();

    [Fact]
    public void WritesTraceParentAndTraceStateFromW3CActivity()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.TraceStateString = "vendor=value";
        activity.Start();

        var metadata = new Dictionary<string, string>();
        _enricher.Enrich(metadata);

        metadata.ShouldContainKeyAndValue("traceparent", activity.Id!);
        metadata.ShouldContainKeyAndValue("tracestate", "vendor=value");
    }

    [Fact]
    public void OmitsTraceStateWhenEmpty()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var metadata = new Dictionary<string, string>();
        _enricher.Enrich(metadata);

        metadata.ShouldContainKey("traceparent");
        metadata.ShouldNotContainKey("tracestate");
    }

    [Fact]
    public void DoesNothingForHierarchicalActivity()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.Hierarchical);
        activity.Start();

        var metadata = new Dictionary<string, string>();
        _enricher.Enrich(metadata);

        metadata.ShouldBeEmpty();
    }

    [Fact]
    public void DoesNothingWhenNoCurrentActivity()
    {
        Activity.Current = null;

        var metadata = new Dictionary<string, string>();
        _enricher.Enrich(metadata);

        metadata.ShouldBeEmpty();
    }
}
