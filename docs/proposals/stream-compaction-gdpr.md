# Proposal: Stream Compaction for GDPR Compliance

## Problem

The event sourcing library stores events immutably in Cosmos DB. When a GDPR Article 17 (right to erasure) request arrives, the PII contained in event bodies must be physically removed from the store. The current soft-delete mechanism (`ISnapshot.DeletedAt`) hides projections from read queries but the underlying events — and the PII they contain — remain in the database.

## Proposed Solution: Tombstone-Based Stream Compaction

Replace a stream's full event history with a single "tombstone" event that reproduces a scrubbed version of the current projection state (PII removed), then physically delete the original events.

### How It Works

1. Read all events in the stream and replay them to build the current projection
2. A consumer-provided compactor creates a tombstone event with PII fields scrubbed
3. Replace the event at position 1 with the tombstone
4. Delete events at positions 2 through N
5. Upsert the snapshot with revision 1

After compaction the stream contains a single event. New events append at position 2 onwards as normal. The tombstone is a regular event — its `Apply` method sets the scrubbed state on a fresh snapshot, so all existing repository and projection machinery continues to work without modification.

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

### Why the Tombstone Must Be at Position 1

`EventStore.AppendToStreamInternal` verifies that the previous event exists before creating a new one (optimistic concurrency via a transactional batch read). Event 2 needs event 1 to exist. Placing the tombstone at position 1 means future appends find it and succeed without any changes to the append logic.

### Revision Reset

After compaction the stream contains one event, so `EventRepository.Get` sets `Revision = events.Count = 1`. Any in-flight writes holding a stale revision will receive a `ConcurrencyException` and must retry. This is acceptable for an administrative operation.

### Change Feed Behaviour

Cosmos DB change feed fires for the tombstone replacement (a document update) but does not fire for deletes in default mode. The `SnapshotProjectionCatchupSubscription` receives the tombstone at EventNumber 1. Since the compaction service has already upserted the snapshot at revision 1, the handler's existing check (`snapshot.Revision >= event.EventNumber`) evaluates to `1 >= 1 = true`, skipping the event. No additional change feed handling is required.

### Batch Size Constraint

Cosmos DB transactional batches are limited to 100 operations. The compaction strategy adapts to stream size:

- **Streams with up to 100 events:** A single transactional batch atomically replaces event 1 with the tombstone and deletes events 2 through N.
- **Streams with more than 100 events:** Delete events in batches of 100 working backwards from the tail, then a final batch replaces event 1 with the tombstone and deletes any remaining events near position 1. Working from the tail means that if the operation is interrupted mid-way, the stream prefix remains valid and the operation can be safely retried.

### Idempotency

If compaction fails partway through (for large streams) and is retried, the retry reads the stream again and only processes events that still exist. Re-replacing the tombstone at position 1 is a no-op in effect. Compacting an already-compacted stream (single event) is also a safe no-op.

### Concurrent Writes During Compaction

If a new event is appended while compaction is deleting events from the tail, the append's concurrency check may fail if the previous event was already deleted, resulting in a `ConcurrencyException`. The caller retries with the correct expected version. This is the same behaviour as any other concurrency conflict and requires no special handling.

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

The tombstone event class itself is a regular event:

```csharp
public class CustomerCompacted(
    CustomerUri customerUri,
    string emailAddress,
    string firstName,
    string lastName,
    DateTimeOffset createdOn
) : CustomerEvent(customerUri)
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

| Type                                             | Purpose                                         |
| ------------------------------------------------ | ----------------------------------------------- |
| `IStreamCompactor<TBaseEvent, TSnapshot>`        | Consumer interface for defining scrubbing logic |
| `StreamCompactionService<TBaseEvent, TSnapshot>` | Orchestrates the compaction flow                |

### Changes to Existing Types

| Type                                        | Change                                                                         |
| ------------------------------------------- | ------------------------------------------------------------------------------ |
| `EventStore<TBaseEvent>`                    | Add `CompactStream` method (transactional batch replace + delete)              |
| `SnapshotRepository<TBaseEvent, TSnapshot>` | Add `UpsertSnapshot` method                                                    |
| `EventSourceConfiguration<TBaseEvent>`      | Add `WithCompaction` builder method                                            |
| `EventSourcingBuilder`                      | Register compaction services in DI                                             |
| `TestTransactionalBatch`                    | Implement `DeleteItem` and `ReplaceItem` (currently `NotImplementedException`) |
| `ContainerMock`                             | Implement `ReplaceItemStreamAsync` (currently `NotImplementedException`)       |

### Test Scenarios

1. Compact single-event stream
2. Compact multi-event stream (verify original events are deleted)
3. Append new events after compaction
4. Compact empty stream (no-op)
5. Compact large stream (>100 events, multi-batch)
6. Idempotent re-compaction
7. Revision resets to 1
8. `ConcurrencyException` on stale revision after compaction
