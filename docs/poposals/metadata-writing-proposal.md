# Proposal: Writing Metadata to Stored Events

## Background

Log Otter's event store already has first-class metadata support end-to-end. Every event carries a `Dictionary<string, string> Metadata` field that is persisted to Cosmos DB, survives reads, and is surfaced to catch-up subscription handlers via `Event<T>.Metadata` and to projection apply methods via `EventInfo.Metadata`.

The gap is on the **write path**. `EventRepository.ApplyEvents()` currently hard-codes an empty metadata dictionary when creating `EventData` instances (line 73, `EventRepository.cs`). There is no mechanism for callers — or ambient infrastructure — to inject metadata such as distributed trace context before events are written.

## Problem

When debugging or tracing requests across services, engineers want to correlate events stored in the event store with the distributed trace that triggered the write. Without metadata on stored events, there is no link between a trace in (e.g.) Jaeger or Honeycomb and the causally-related events in Cosmos DB.

Fixing this by having every call-site manually construct and pass a metadata dictionary is error-prone and creates noise in business logic — callers should not have to know about tracing infrastructure.

## Goals

- Automatically capture the current W3C trace context (`traceparent` / `tracestate`) and write it into event metadata at append time.
- Provide a general-purpose extension point so that other metadata (e.g. user identity, tenant ID, correlation IDs) can be injected by application code without modifying the library.
- Require zero changes to existing call-sites for the common (trace context only) case.
- Introduce no mandatory new package dependencies.

## Non-goals

- Propagating metadata from stored events back into outgoing requests when events are replayed (a separate concern for handlers).
- Defining a fixed schema or reserved keys for metadata fields (beyond the trace context convention below).

---

## Proposed Design

### Core interface: `IEventMetadataEnricher`

A new interface in `LogOtter.CosmosDb.EventStore`:

```csharp
public interface IEventMetadataEnricher
{
    void Enrich(IDictionary<string, string> metadata);
}
```

`EventRepository` resolves `IEnumerable<IEventMetadataEnricher>` from DI and calls each one to build a metadata dictionary before constructing `EventData` instances. Because enrichers run once per `ApplyEvents` call (not per individual event), all events in a single batch share the same metadata — which is correct, since they all originate from the same request/trace.

### Changes to `EventRepository`

`EventRepository` gains a constructor parameter `IEnumerable<IEventMetadataEnricher>? metadataEnrichers = null`. The default value keeps existing **manual** construction (e.g. in tests) compiling; the `null` is coalesced to an empty collection. No change is needed in `EventSourcingBuilder` — `EventRepository` is registered as an open-generic singleton, and the DI container automatically resolves `IEnumerable<IEventMetadataEnricher>` (an empty enumerable when nothing is registered).

In both `ApplyEvents` and `ApplyAndGetEvents`, the hard-coded `new EventData<TBaseEvent>(Guid.NewGuid(), e, now)` call becomes:

```csharp
var metadata = BuildMetadata();
var eventData = events.Select(e => new EventData<TBaseEvent>(Guid.NewGuid(), e, now, metadata)).ToArray();
```

where `BuildMetadata()` invokes each registered enricher:

```csharp
private Dictionary<string, string> BuildMetadata()
{
    var metadata = new Dictionary<string, string>();
    foreach (var enricher in _metadataEnrichers)
        enricher.Enrich(metadata);
    return metadata;
}
```

All events in the batch share the same `metadata` dictionary reference. This is safe because the dictionary is never mutated after `BuildMetadata()` returns — enrichers run only during the build.

The `Apply()` call site on line 70 also passes the metadata snapshot so the in-memory projection sees the same values:

```csharp
eventToApply.Apply(entity, new(now, ++revision, metadata));
```

### Built-in enricher: `ActivityMetadataEnricher`

`System.Diagnostics.Activity` is part of the BCL (no additional packages required). The enricher reads the current activity's W3C trace context and writes the standard header names:

```csharp
public sealed class ActivityMetadataEnricher : IEventMetadataEnricher
{
    public void Enrich(IDictionary<string, string> metadata)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        // W3C traceparent: 00-{traceId}-{spanId}-{flags}
        metadata["traceparent"] = activity.Id!;

        if (!string.IsNullOrEmpty(activity.TraceStateString))
            metadata["tracestate"] = activity.TraceStateString;
    }
}
```

This integrates transparently with any OTel SDK that uses `ActivitySource` (the standard .NET OTel approach), as well as with `HttpClient` propagation via the built-in `DistributedContextPropagator`.

### Registration API

```csharp
// Register just the Activity enricher (trace context only)
builder.Services.AddEventMetadataEnricher<ActivityMetadataEnricher>();

// Or add custom enrichers alongside it
builder.Services.AddEventMetadataEnricher<TenantMetadataEnricher>();
```

`AddEventMetadataEnricher<T>` is a simple extension method that calls `services.AddSingleton<IEventMetadataEnricher, T>()`.

The `ActivityMetadataEnricher` is **opt-in** — it is not registered by default, so existing deployments are unaffected until they add the registration.

---

## Alternative: Explicit metadata parameter

As a secondary, complementary mechanism, overloads of `ApplyEvents` and `ApplyAndGetEvents` can accept a caller-supplied `IReadOnlyDictionary<string, string>? additionalMetadata` parameter. The repository merges this with enricher output (enrichers run first; caller-supplied values override on key collision).

This is useful for:
- Integration tests that want to assert specific metadata is stored.
- Application code that has metadata not available via ambient context (e.g. idempotency keys).

This is lower priority than the enricher mechanism — the enricher approach covers the tracing use case without any call-site changes.

---

## Key metadata names (convention)

| Key | Value | Source |
|---|---|---|
| `traceparent` | W3C traceparent string | `Activity.Current.Id` |
| `tracestate` | W3C tracestate string | `Activity.Current.TraceStateString` |

These follow the [W3C Trace Context](https://www.w3.org/TR/trace-context/) specification, making the values directly interpretable by any W3C-compatible tracing backend.

---

## Affected files

| File | Change |
|---|---|
| `Repositories/EventRepository.cs` | Accept `IEnumerable<IEventMetadataEnricher>?`, add `additionalMetadata` overloads, call `BuildMetadata()` before creating `EventData` and pass metadata to `Apply()` |
| `MetadataEnrichers/IEventMetadataEnricher.cs` (new) | Define the enricher interface |
| `MetadataEnrichers/ActivityMetadataEnricher.cs` (new) | Built-in W3C trace context enricher (guards on `IdFormat == W3C`) |
| `MetadataEnrichers/EventMetadataEnricherExtensions.cs` (new) | `AddEventMetadataEnricher<T>()` helper (uses `TryAddEnumerable`) |
| `sample/CustomerApi/Program.cs` | Demonstrates opt-in registration of `ActivityMetadataEnricher` |

No changes to `EventSourcingBuilder` (DI auto-resolves the enricher collection), the storage layer (`EventStore`, `CosmosDbStorageEvent`) or the read path — the metadata plumbing already exists there.

---

## Resolved decisions

1. **Package placement** — `ActivityMetadataEnricher` lives in the core `LogOtter.CosmosDb.EventStore` package. It has no extra dependencies (`Activity` is BCL), so a separate package was not warranted.

2. **Enricher ordering** — Registration order is sufficient; the interface exposes no priority property. Caller-supplied `additionalMetadata` (below) always wins on key collision regardless of enricher order.

3. **Per-event vs per-batch metadata** — Per-batch: all events in a batch share one metadata snapshot, since they originate from the same request/trace.

4. **Apply() metadata** — `EventInfo.Metadata` **is** populated with the write-time metadata during in-memory apply, for consistency with the read path. (Note: this metadata reflects what *will* be written; the `AppendToStream` call could still fail afterwards.)

5. **Enricher lifetime** — Enrichers are registered as singletons. To read per-request state, an enricher should depend on an ambient accessor (`Activity.Current`, `IHttpContextAccessor`) and resolve it inside `Enrich()`, rather than capturing a scoped service.

## Security note

Event metadata is persisted to Cosmos DB and is readable by anyone with access to the container. It is **not** currently surfaced by the EventStreams API response DTO. Because the enricher extension point accepts arbitrary data, callers must not write secrets, credentials or sensitive personal data into metadata. W3C trace context is non-sensitive.
