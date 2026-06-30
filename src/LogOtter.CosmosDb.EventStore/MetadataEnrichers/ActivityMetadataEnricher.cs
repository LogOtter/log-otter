using System.Diagnostics;

namespace LogOtter.CosmosDb.EventStore;

/// <summary>
/// Writes the current <see cref="Activity"/>'s W3C trace context into event metadata, so stored events
/// can be correlated with the distributed trace that produced them.
/// </summary>
/// <remarks>
/// Only emits values when the current activity uses the W3C id format (the .NET default). Hierarchical
/// (legacy) activities are ignored because their <see cref="Activity.Id"/> is not a valid <c>traceparent</c>.
/// </remarks>
public sealed class ActivityMetadataEnricher : IEventMetadataEnricher
{
    public void Enrich(IDictionary<string, string> metadata)
    {
        var activity = Activity.Current;
        if (activity is null || activity.IdFormat != ActivityIdFormat.W3C)
        {
            return;
        }

        // W3C traceparent: 00-{traceId}-{spanId}-{flags}
        if (!string.IsNullOrEmpty(activity.Id))
        {
            metadata["traceparent"] = activity.Id;
        }

        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            metadata["tracestate"] = activity.TraceStateString;
        }
    }
}
