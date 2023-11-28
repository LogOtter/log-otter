# CosmosDB EventStore

A lightweight event sourcing abstraction for CosmosDB, built on top of LogOtter.CosmosDb.

# Setup

First set up [LogOtter.CosmosDb](https://github.com/LogOtter/log-otter/tree/main/src/LogOtter.CosmosDb).

Chain a call to `AddEventSourcing`:

```csharp
services
    .AddCosmosDb()
    .WithAutoProvisioning()
    .AddEventSourcing(options => options.AutoEscapeIds = true)
```

We recommend setting AutoEscapeIds = true to ensure that any usage of [invalid characters](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.resource.id?view=azure-dotnet) does not result in runtime errors or documents that are difficult to remove.

# Adding an event source

Chain a call to `.AddEventSource<T>` where `T` is your base event type (all events for this store must extend this base type), specifying the container name.

```csharp
services
    .AddCosmosDb()
    .WithAutoProvisioning()
    .AddEventSourcing(options => options.AutoEscapeIds = true)
    .AddEventSource<CustomerEvent>("Customers")
```

Once added, you can request the dependency from the DI container and pull out the underlying EventStore.

```csharp
using LogOtter.CosmosDb.EventStore;
using Microsoft.Azure.Cosmos;

namespace Customer;

public class CustomerEventRepo
{
    private readonly EventStore _customerEventStore;

    public CustomerEventRepo(EventStoreDependency<CustomerEvent> customerEventStoreDependency)
    {
        _customerEventStore = customerEventStoreDependency.EventStore;
    }
}
```

# Adding projection(s)

In many cases, a projection makes working with events easier.

Add a projection when configuring the event source by calling `AddProjection<T>()` where `T` is the type of the projection.

```csharp
services
    .AddCosmosDb()
    .WithAutoProvisioning()
    .AddEventSourcing(options => options.AutoEscapeIds = true)
    .AddEventSource<CustomerEvent>("CustomerEvents", c =>
    {
        c.AddProjection<CustomerReadModel>();
    });
```

You will now be able to request an `EventRepository<TBaseEvent, TProjection>` from the DI container:

```csharp
using LogOtter.CosmosDb.EventStore;
using Microsoft.Azure.Cosmos;

namespace Customer;

public class CustomerRepo
{
    private readonly EventRepository<CustomerEvent, CustomerReadModel> _customerEventRepo;

    public CustomerRepo(EventRepository<CustomerEvent, CustomerReadModel> customerEventRepo)
    {
        _customerEventRepo = customerEventRepo;
    }
}
```

# Adding a snapshot store

When using projections, the events are read from the store and projected to the projection type every time they are requested. As the number of events grows, it may be more performant to save the projection to its own store and read from that.

To store a snapshot, chain a call to `.WithSnapshot` when adding the projection, specifying the container name to be used for it, as well as an expression to select the partition key.

```csharp
services
    .AddCosmosDb()
    .WithAutoProvisioning()
    .AddEventSourcing(options => options.AutoEscapeIds = true)
    .AddEventSource<CustomerEvent>("CustomerEvents", c =>
    {
        c.AddProjection<CustomerReadModel>()
            .WithSnapshot("Customers", _ => CustomerReadModel.StaticPartitionKey);
    });
```

When adding the snapshot, the auto-provisioning functionality is the same as in the CosmosDb package.

If you want to override the settings you can do so by chaining a call to `.WithAutoProvisionSettings` (see the [LogOtter.CosmosDb](https://github.com/LogOtter/log-otter/tree/main/src/LogOtter.CosmosDb) documentation for more information)

```csharp
services
    .AddCosmosDb()
    .WithAutoProvisioning()
    .AddEventSourcing(options => options.AutoEscapeIds = true)
    .AddEventSource<CustomerEvent>("CustomerEvents", c =>
    {
        c.AddProjection<CustomerReadModel>()
            .WithSnapshot("Customers", _ => CustomerReadModel.StaticPartitionKey)
            .WithAutoProvisionSettings(
                compositeIndexes: new[]
                {
                    new Collection<CompositePath>
                    {
                        new() { Path = "/LastName", Order = CompositePathSortOrder.Ascending },
                        new() { Path = "/FirstName", Order = CompositePathSortOrder.Ascending }
                    }
                }
            );
    });
```

You will now be able to request a SnapshotRepository<TBaseEvent, TProjection> from the DI container:

```csharp
using LogOtter.CosmosDb.EventStore;
using Microsoft.Azure.Cosmos;

namespace Customer;

public class CustomerRepo
{
    private readonly SnapshotRepository<CustomerEvent, CustomerReadModel> _customerSnapshots;

    public CustomerRepo(SnapshotRepository<CustomerEvent, CustomerReadModel> customerSnapshots)
    {
        _customerSnapshots = customerSnapshots;
    }
}
```

Note that by adding the snapshot, you'll also get a background change feed processor that updates the snapshot every time new events are added. Because of this, you must use auto-provisioning or manually provision a Leases container.

# Adding a custom catch up subscription

You may want to take certain actions when an event is appended to a stream, for example, publishing an event over a transport such as Azure Service Bus. You can do this with a catch up subscription.

**Catch up subscriptions use the CosmosDB change feed behind the scenes.**

To add a custom catch up subscription when configuring the event source, call `.AddCatchupSubscription`. You'll need to provide the type of your event processing class and a name (the name is used in the Leases container to keep track of processing).

```csharp
services
    .AddCosmosDb()
    .WithAutoProvisioning()
    .AddEventSourcing(options => options.AutoEscapeIds = true)
    .AddEventSource<CustomerEvent>("CustomerEvents", c =>
    {
        c.AddCatchupSubscription<CustomerEventPublisher>("CustomerEventCatchupSubscription");
    });
```

Your catch up subscription processor would look something like this:

```csharp
using LogOtter.CosmosDb.EventStore;

namespace Customer;

public class CustomerEventPublisher : ICatchupSubscription<CustomerEvent>
{
    private readonly AsbPublisher _asbPublisher;

    public CustomerEventPublisher(AsbPublisher asbPublisher)
    {
        _asbPublisher = asbPublisher;
    }

    public async Task ProcessEvents(IReadOnlyCollection<Event<CustomerEvent>> events, CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        { 
            await _asbPublisher.PublishEvent(@event);
        }

        return Task.CompletedTask;
    }
}

```

# Usage

Depending on the setup steps you have used, you'll have one or more of the following:

- `EventStoreDependency<TBaseEvent>`
- `EventRepository<TBaseEvent, TProjection>`
- `SnapshotRepository<TBaseEvent, TProjection>`

## EventStoreDependency<TBaseEvent>

This is the lowest level dependency you can work with. The other abstractions are built on top of this. You'll need to get the underlying `EventStore`, which provides two methods of interest.

### AppendToStream
`AppendToStream` will append events to the end of the specified stream. You'll need to wrap them in `EventData`, and if its an existing stream you'll need to know the current revision. If the expectedRevision already exists you'll get a concurrency error.

```csharp
var eventData = events.Select(e => new EventData(Guid.NewGuid(), e, e.Ttl ?? -1)).ToArray();

await _eventStore.AppendToStream(streamId, expectedRevision ?? 0, cancellationToken, eventData);
```

### ReadStreamForwards
`ReadStreamForwards` will read the stream, starting at the specified event, or the first event if one isn't specified. It will read the number of events specified, or all remaining events to the end of the stream if not specified.

Read all events from the start:

```csharp
var eventStoreEvents = await _eventStore.ReadStreamForwards(streamId, cancellationToken);
```

Read 12 events, starting at event 5:
```csharp
var eventStoreEvents = await _eventStore.ReadStreamForwards(
    streamId,
    5,
    12,
    cancellationToken);
```

Both methods will return a collection of `StorageEvent`, if you want the underlying `TBaseEvent` you can do something like:

```csharp
var events = eventStoreEvents.Select(e => (TBaseEvent) e.EventBody);
```

# EventRepository<TBaseEvent, TProjection>

EventRepository provides an abstraction over the event store when used with a projection. If you're using projections, we recommend you do not use the lower level `EventStoreDependency<TBaseEvent>`, and instead use `EventRepository<TBaseEvent, TProjection>`.

### Get

You can read an event stream and project the events in memory to the projection type (nullable). A controller could look like:

```csharp
private readonly EventRepository<CustomerEvent, CustomerReadModel> _customerEventRepository;

// ... initialise from ctor ...

public async Task<ActionResult<CustomerResponse>> GetCustomer(
    string customerUri,
    CancellationToken cancellationToken)
{
    // Type will be: CustomerReadModel?
    var customer = await _customerEventRepository.Get(
        customerUri,
        cancellationToken: cancellationToken);
    
    return customer == null
        ? NotFound()
        : Ok(CustomerResponse.FromReadModel(customer)); // We recommend using response types
}
```

### ApplyEvents

You can apply events to the stream by providing the streamId, the expected revision (`0` if this is a new stream) and the events. If you have more than 1 event you can pass them in all at once and they'll be batched.

If the stream already exists and the expectedRevision doesn't match the current revision, you'll get a concurrency error.

The result of the `ApplyEvents` call is the projected type.

```csharp
var customerUri = "/customers/customer123";

// CustomerCreated extends CustomerEvent
var customerCreated = new CustomerCreated(
    customerUri,
    customerData.EmailAddress,
    customerData.FirstName,
    customerData.LastName);

var customer = await _customerEventRepository.ApplyEvents(
    customerUri,
    0,
    cancellationToken,
    customerCreated);
```

### GetEventStream

This method is a thin layer on top of the `EventStore`'s `ReadStreamForwards` method. It will read the whole stream and extract the `TBaseEvent` from the `StorageEvent`, giving you the full stream of `TBaseEvent`:

```csharp
var customerEvents = await _customerEventRepository.GetEventStream(
    customerUri,
    cancellationToken);
```

# SnapshotRepository<TBaseEvent, TProjection>

One of the benefits of having a snapshot is that the data store can be queried efficiently if the relevant indices and partitioning strategy have been used.

The `SnapshotRepository<TBaseEvent, TProjection>` provides some further abstraction over `EventRepository<TBaseEvent, TProjection>` and adds methods to count and query.

### GetSnapshot

Get the snapshot. An important nuance of using the snapshot store is that the snapshot could be behind the event stream. This is because the stream is processed asynchronously when events are appended to it.

```csharp
private readonly SnapshotRepository<CustomerEvent, CustomerReadModel> _customerSnapshotRepository;

// ... initialise from ctor ...

public async Task<ActionResult<CustomerResponse>> GetCustomer(
    string customerUri,
    CancellationToken cancellationToken)
{
    // Type will be: CustomerReadModel?
    var customer = await _customerSnapshotRepository.GetSnapshot(
        customerUri.Uri,
        CustomerReadModel.StaticPartitionKey,
        cancellationToken: cancellationToken)
    
    return customer == null
        ? NotFound()
        : Ok(CustomerResponse.FromReadModel(customer));
}
```

### CountSnapshotsAsync

Count the snapshots in the store.

**Count of all snapshots**

```csharp
var totalCount = await _customerSnapshotRepository.CountSnapshotsAsync(
    CustomerReadModel.StaticPartitionKey,
    cancellationToken: cancellationToken
);
```

**Count of snapshots matching a query**

```csharp
var dibleyPartyCount = await _customerSnapshotRepository.CountSnapshotsAsync(
    CustomerReadModel.StaticPartitionKey,
    q => q.Where(c => c.LastName == "Dibley"),
    cancellationToken: cancellationToken
);
```

### QuerySnapshots

Get snapshots by querying the container.

**Get all snapshots**

We don't recommend this unless you know you will only have a small number of snapshots in the partition.

```csharp
var customers = await _customerSnapshotRepository
    .QuerySnapshots(CustomerReadModel.StaticPartitionKey, cancellationToken: cancellationToken)
    .ToListAsync();
```

**Get snapshots matching a query**

```csharp
var dibleyParty = await _customerSnapshotRepository
    .QuerySnapshots(
        CustomerReadModel.StaticPartitionKey,
        q => q.Where(c => c.LastName == "Dibley"),
        cancellationToken: cancellationToken)
    .ToListAsync();
```

You can use other query operators such as `OrderBy`:
```csharp
var dibleyParty = await _customerSnapshotRepository
    .QuerySnapshots(
        CustomerReadModel.StaticPartitionKey,
        q => q.Where(c => c.LastName == "Dibley").OrderBy(c => c.FirstName),
        cancellationToken: cancellationToken)
    .ToListAsync();
```
