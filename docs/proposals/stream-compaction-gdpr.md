# Proposal: Stream Compaction for GDPR Compliance

## Problem

The event sourcing library stores events immutably in Cosmos DB. When a GDPR Article 17 (right to erasure) request arrives, the PII contained in event bodies must be physically removed from the store. The current soft-delete mechanism (`ISnapshot.DeletedAt`) hides projections from read queries but the underlying events — and the PII they contain — remain in the database.

## Proposed Solution: Tombstone-Based Stream Compaction

Replace a stream's full event history with a single "tombstone" event that reproduces a scrubbed version of the current projection state (PII removed), then physically delete the original events.

### How It Works

1. Read all events in the stream and replay them to build the current projection
2. A consumer-provided compactor creates a tombstone event with PII fields scrubbed
3. In one atomic transactional batch, replace event 1 with the tombstone **and** delete up to 99 of the highest-numbered tail events
4. Delete any remaining tail events in additional 100-op batches
5. Upsert the snapshot at revision 1

After compaction the stream contains a single event: the tombstone at position 1. The tombstone implements `ICompactionEvent`, which all event-replay paths treat as a hard reset of the projection — they apply it and stop, ignoring anything that follows.

### Example

Before compaction:

```
Stream "customer-123":
  Event 1: CustomerCreated { Email: "alice@example.com", Name: "Alice Smith" }
  Event 2: CustomerNameChanged { Name: "Alice Jones" }
  Event 3: CustomerEmailChanged { Email: "alice.jones@example.com" }
```

After compaction:

```
Stream "customer-123":
  Event 1: CustomerCompacted { Email: "[REDACTED]", Name: "[REDACTED]", CreatedOn: 2025-01-15 }
```

The consumer controls exactly which fields are scrubbed and which are preserved.

## Design Details

### Tombstone-First Ordering

The tombstone is written **before** any events are deleted, in the same atomic batch as the first round of tail deletes. This is the critical invariant that makes crash recovery safe.

If we deleted tail events first and only replaced event 1 in a final batch, a crash mid-way would leave a truncated stream: events 1..K survive but K..N are gone. Retrying would re-derive the projection from the truncated stream and produce a tombstone reflecting **stale state** (the projection at event K, not at event N). Reversing the order guarantees that any post-crash state always has the correctly-derived tombstone at position 1, with leftover events confined to the tail awaiting cleanup.

### `ICompactionEvent` and Read-Path Filtering

Tombstone events implement the marker interface `ICompactionEvent`. Three read paths recognise it:

- `EventRepository.Get` — applies the tombstone, sets `Revision = 1`, and breaks out of the apply loop.
- `SnapshotRepository.ApplyEventsToSnapshot` — discards the in-progress snapshot, instantiates a fresh one, applies the tombstone, and breaks. This guarantees that a change-feed delivery of the tombstone correctly replaces a stale pre-compaction snapshot regardless of its current revision.
- `HybridRepository.ApplyNewEvents` — same reset-and-break semantics.

This filtering also protects the read path during the cleanup window. Between Phase A (tombstone-first batch) and Phase B (remaining tail deletes), the stream contains `[tombstone, leftover2, ..., leftoverK]`. Without the filter, applying tombstone + leftovers in sequence would corrupt the projection. With the filter, reads return the correct (tombstone-only) state throughout the cleanup window.

### Why the Tombstone Must Be at Position 1

`EventStore.AppendToStreamInternal` verifies that the previous event exists before creating a new one (optimistic concurrency via a transactional batch read). Event 2 needs event 1 to exist. Placing the tombstone at position 1 means the raw append machinery is unchanged.

### Revision Reset

`EventRepository.Get` returns a snapshot with `Revision = 1` when it encounters a tombstone, regardless of how many leftover events still exist in the stream. The compaction service also upserts the snapshot at revision 1 directly. Any in-flight writes holding a stale revision will receive a `ConcurrencyException` on their next append and must retry.

### Post-Compaction Appends Are Ignored by Projections

This is a deliberate trade-off of the tombstone-first design. The raw event store accepts appends at position 2+ after compaction (the previous-event check passes because event 1 exists), but all projection read paths stop at the tombstone. New events written after compaction land in storage but are invisible to consumers reading the projection.

The reason: with tombstone-first ordering, mid-cleanup the stream looks identical to "compaction complete + user appended a new event" — both produce `[tombstone, eventAt2]`. There's no metadata to distinguish a leftover from a legitimate post-compaction append, so the read path conservatively treats all events after the tombstone as leftovers.

For the GDPR use case this is the right semantic: once a customer's stream is compacted, the customer is logically gone and no further activity should be expected. Streams that need to remain live after compaction are out of scope for this design.

### Change Feed Behaviour

Cosmos DB change feed fires for the tombstone replacement (a document update) but does not fire for deletes in default mode. The `SnapshotProjectionCatchupSubscription` receives the tombstone at EventNumber 1.

`SnapshotRepository.ApplyEventsToSnapshot` special-cases `ICompactionEvent`: when one is encountered, it discards the current snapshot (which may still be at the stale pre-compaction revision) and rebuilds it from the tombstone alone. This fires regardless of the existing `Revision >= EventNumber` short-circuit, so the catchup subscription correctly converges the snapshot even if the compaction service crashed before its own upsert.

### Batch Size Constraint

Cosmos DB transactional batches are limited to 100 operations. The compaction strategy adapts to stream size:

- **Streams with up to 100 events:** A single transactional batch atomically replaces event 1 with the tombstone and deletes events 2 through N.
- **Streams with more than 100 events:** Phase A — one transactional batch atomically replaces event 1 with the tombstone **and** deletes up to 99 of the highest-numbered tail events. Phase B — remaining tail events are deleted in subsequent 100-op batches.

If the process crashes during Phase B, the stream's first event is already the tombstone, so the read path returns the correct projection immediately. On retry, the compaction service detects the existing tombstone and resumes cleanup without re-deriving the projection.

### Idempotency

When `StreamCompactionService.CompactStream` runs against a stream whose first event is already an `ICompactionEvent`, it reuses the existing tombstone verbatim — same `EventId`, `CreatedOn`, and `Metadata` — instead of re-deriving the projection. This is essential after a partial-cleanup crash: re-deriving from a truncated stream would produce a stale tombstone.

Phase A's replace operation is idempotent in effect (the existing tombstone is rewritten with the same content), and Phase B simply deletes whatever leftover events remain. Compacting an already-fully-compacted stream is therefore a safe no-op apart from one redundant document write.

### Concurrent Writes During Compaction

If a new event is appended while compaction is running, two outcomes are possible:

- The append's previous-event check finds a now-deleted predecessor → `ConcurrencyException`.
- The append succeeds and lands at some position > 1. Once compaction completes, that event is in the stream but is treated as a post-compaction leftover by the read path (see *Post-Compaction Appends Are Ignored by Projections*) and is removed by the next compaction run.

Callers are expected to retry on `ConcurrencyException`. Operationally, compaction is a controlled administrative operation; concurrent writes during compaction are not the expected case.

## Consumer API

### Interface

Consumers implement `IStreamCompactor<TBaseEvent, TSnapshot>` to define their scrubbing logic:

```csharp
public interface IStreamCompactor<TBaseEvent, TSnapshot>
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    TBaseEvent CreateTombstoneEvent(TSnapshot currentProjection, string streamId);
}
```

### Consumer Implementation Example

```csharp
public class CustomerStreamCompactor : IStreamCompactor<CustomerEvent, CustomerReadModel>
{
    public CustomerEvent CreateTombstoneEvent(CustomerReadModel current, string streamId)
    {
        return new CustomerCompacted(
            current.CustomerUri,
            emailAddress: "[REDACTED]",
            firstName: "[REDACTED]",
            lastName: "[REDACTED]",
            createdOn: current.CreatedOn
        );
    }
}
```

The tombstone event implements both the consumer's event base class and `ICompactionEvent`:

```csharp
public class CustomerCompacted(
    CustomerUri customerUri,
    string emailAddress,
    string firstName,
    string lastName,
    DateTimeOffset createdOn
) : CustomerEvent(customerUri), ICompactionEvent
{
    public override void Apply(CustomerReadModel model, EventInfo eventInfo)
    {
        model.CustomerUri = CustomerUri;
        model.EmailAddress = emailAddress;
        model.FirstName = firstName;
        model.LastName = lastName;
        model.CreatedOn = createdOn;
    }
}
```

### Configuration

Compaction is opt-in per event source:

```csharp
services.AddCosmosDb(config => config
    .AddEventSourcing(es => es
        .AddEventSource<CustomerEvent>("customer-events", config =>
        {
            config.AddProjection<CustomerReadModel>()
                .WithSnapshot("customer-snapshots", e => CustomerReadModel.StaticPartitionKey);

            config.WithCompaction<CustomerStreamCompactor, CustomerReadModel>();
        })
    )
);
```

### Usage

```csharp
public class GdprDeletionService(
    StreamCompactionService<CustomerEvent, CustomerReadModel> compactionService)
{
    public async Task HandleDeletionRequest(string customerId, CancellationToken ct)
    {
        await compactionService.CompactStream(customerId, ct);
    }
}
```

## Implementation Scope

### New Types

| Type                                             | Purpose                                                                                |
| ------------------------------------------------ | -------------------------------------------------------------------------------------- |
| `ICompactionEvent`                               | Marker interface — signals to read paths that this event is a stream-compaction tombstone |
| `IStreamCompactor<TBaseEvent, TSnapshot>`        | Consumer interface for defining scrubbing logic                                        |
| `StreamCompactionService<TBaseEvent, TSnapshot>` | Orchestrates the compaction flow                                                       |

### Changes to Existing Types

| Type                                        | Change                                                                                                  |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `EventStore<TBaseEvent>`                    | Add `CompactStream` method (tombstone-first atomic batch + tail-cleanup batches)                        |
| `EventRepository<TBaseEvent, TSnapshot>`    | `Get` stops applying at the first `ICompactionEvent` and forces `Revision = 1`                          |
| `SnapshotRepository<TBaseEvent, TSnapshot>` | Add `UpsertSnapshot`; `ApplyEventsToSnapshot` resets the snapshot when it encounters an `ICompactionEvent` |
| `HybridRepository<TBaseEvent, TSnapshot>`   | `ApplyNewEvents` stops applying at the first `ICompactionEvent`                                         |
| `EventSourceConfiguration<TBaseEvent>`      | Add `WithCompaction` builder method                                                                     |
| `EventSourcingBuilder`                      | Register compaction services in DI                                                                      |
| `TestTransactionalBatch`                    | Implement `DeleteItem` and `ReplaceItem` (previously `NotImplementedException`)                         |
| `ContainerMock`                             | Implement `ReplaceItemStreamAsync` (previously `NotImplementedException`)                               |

### Test Scenarios

1. Compact single-event stream
2. Compact multi-event stream (verify original events are deleted)
3. Compact empty stream (no-op)
4. Compact large stream (>100 events, multi-batch)
5. Idempotent re-compaction
6. Revision resets to 1
7. `ConcurrencyException` on stale revision after compaction
8. Compaction upserts snapshot at revision 1
9. Retry after partial cleanup reuses the original tombstone (does not re-derive from a truncated stream)
10. Leftovers after the tombstone do not corrupt projections
11. Post-compaction appends are accepted by the raw store but ignored by projections
