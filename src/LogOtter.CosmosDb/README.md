# CosmosDB

A collection of helpers for using CosmosDb in a strongly typed way with dependency injection.

# Setup

To configure CosmosDbOptions, you can either use the options pattern or the setup action. The options depend on your preferred connection method: Managed Identity (MI) or connection string.

### Options pattern

```csharp
services.Configure<CosmosDbOptions>(configuration.GetSection("CosmosDb"));
``` 

### Setup Action

```csharp
builder.Services.AddCosmosDb(options =>
{
    // Configure your CosmosDbOptions here (see below for examples)
});
```

## Managed Identity

Set `ManagedIdentityOptions` with an `AccountEndpoint` and specify the `DatabaseId`. For more information on Managed Identity, refer to the [Microsoft documentation](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/tutorial-vm-managed-identities-cosmos). Ensure that your application has the appropriate MI setup and roles configured.

**System Assigned Managed Identity**

If `UserAssignedManagedIdentityClientId` is not specified, the system-assigned identity will be used.

```csharp
builder.Services.AddCosmosDb(options =>
{
    var accountEndpoint = builder.Configuration["COSMOS_ENDPOINT"];
    
    options.DatabaseId = "Customers";
    options.ManagedIdentityOptions = new ManagedIdentityOptions
    {
       AccountEndpoint = accountEndpoint
    };
});
```

**User Assigned Managed Identity**

Provide `UserAssignedManagedIdentityClientId` to use the specified user-assigned identity.

```csharp
builder.Services.AddCosmosDb(options =>
{
    var clientId = builder.Configuration["COSMOS_CLIENT_ID"];
    var accountEndpoint = builder.Configuration["COSMOS_ENDPOINT"];
    
    options.DatabaseId = "Customers";
    options.ManagedIdentityOptions = new ManagedIdentityOptions
    {
       AccountEndpoint = accountEndpoint,
       UserAssignedManagedIdentityClientId = clientId
    };
});
```

## Connection string

Although using Managed Identity is recommended, you can use a connection string by setting `ConnectionString` and `DatabaseId`.

```csharp
builder.Services.AddCosmosDb(options =>
{
    var cosmosConnectionString = builder.Configuration["COSMOS_CONNECTION_STRING"];
    
    options.DatabaseId = "Customers";
    options.ConnectionString = cosmosConnectionString;
});
```

## Auto Provisioning

By default, the library expects the database and containers to exist. To automatically provision the database and containers, chain a call to `.WithAutoProvisioning()`:

```csharp
services
    .AddCosmosDb()
    .WithAutoProvisioning()
```
> Note: The database and containers will be auto provisioned asynchronously as the application starts. A health check is automatically added that will not report healthy until the database has provisioned.

If synchronous database provisioning is required before the application starts, then call

```csharp
app.ProvisionCosmosDb();
```

## Adding Containers

Before performing useful operations, add a container using `.AddContainer<T>()` where `T` is the type you want to store. Each container can store one type of object, preventing mixed document types in a container.

### Auto Provisioning Settings

The default container settings when you have called `.WithAutoProvisioning()` are:

- Partition key path `/partitionKey`
- Default ttl `-1`
- All other settings will be the defaults usually used when calling `CreateContainerIfNotExistsAsync` (from the Cosmos SDK)

To override the defaults, specify them explicitly:

```csharp
services
    .AddCosmosDb()
    .AddContainer<Customer>(
        "Customers",
        configure =>
        {
            configure.WithAutoProvisionSettings(
                partitionKeyPath: "/someOtherPartitionKey",
                uniqueKeyPolicy: new()
                { 
                    UniqueKeys = 
                    {
                        new()
                        {
                            Paths = { "/customerNationalInsuranceNumber" }
                        }
                    }                    
                },
                defaultTimeToLive: 3600,
                compositeIndexes: new[]
                {
                    new Collection<CompositePath>
                    {
                        new() { Path = "/LastName", Order = CompositePathSortOrder.Ascending },
                        new() { Path = "/FirstName", Order = CompositePathSortOrder.Ascending }
                    }
                },
                throughputProperties: ThroughputProperties.CreateAutoscaleThroughput(1000)
            );
        }
    )
```

# Usage

Inject `CosmosContainer<T>` into your classes, where `T` is your model type, and use the underlying `Container`.

```csharp
using LogOtter.CosmosDb;
using Microsoft.Azure.Cosmos;

namespace MyRepo;

public class MyRepo
{
    // This is a Container from the Microsoft.Azure.Cosmos namespace
    private readonly Container _container;
    
    public MyRepo(CosmosContainer<Customer> logOtterContainer)
    {
        _container = logOtterContainer.Container;
    }
    
    // You can now use the container as you would any other
}
```

## Multiple containers

You can register multiple containers in your setup:

```csharp
services
    .AddCosmosDb()
    .AddContainer<Customer>("Customers")
    .AddContainer<Widget>("Widgets")
```

Since `CosmosContainer` is strongly typed, you can request more than one from the DI container:

```csharp
using LogOtter.CosmosDb;
using Microsoft.Azure.Cosmos;

namespace MyRepo;

public class MyRepo
{
    private readonly Container _customerContainer;
    private readonly Container _widgetContainer;
    
    public MyRepo(
        CosmosContainer<Customer> customerLogOtterContainer,
        CosmosContainer<Widget> widgetLogOtterContainer)
    {
        _customerContainer = customerLogOtterContainer.Container;
        _widgetContainer = widgetLogOtterContainer.Container;
    }
    
    // You can now use the containers as you would any other
}
```
