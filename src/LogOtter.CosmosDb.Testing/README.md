# CosmosDB Testing

Overrides for [CosmosDb](../LogOtter.CosmosDb) that uses [CosmosDb ContainerMock](../LogOtter.CosmosDb.ContainerMock)
for use in testing.

Note that when we refer to "live" configuration, we mean the setup your implementation code is using (e.g. in Program.cs), as opposed to your testing code.

Nothing connects to Cosmos DB in tests, so the connection options such as `ConnectionString` and `DatabaseId` don't need to be set.

## Containers

The live config decides how the containers will exist when the application is deployed, and the test setup must reflect that decision:

- **Created by the application:** if the live config uses `.WithAutoProvisioning()`, the application creates its containers when it starts. Tests create them in the same way, using each container's auto provisioning settings, so the test setup doesn't need to do anything. Pre-provisioning one of these containers in the test setup throws.
- **Created outside the application:** if the live config doesn't use `.WithAutoProvisioning()`, the containers must already exist when the application is deployed, for example because they're created through infrastructure as code (IaC). Pre-provisioning a container in the test setup is how you state this, so every container registered in the live config must be pre-provisioned with the same settings as the real container, or it throws:

```csharp
services
    .AddTestCosmosDb()
    .WithPreProvisionedContainer<Customer>("Customers", partitionKeyPath: "/customerId");
```

Pre-provisioned containers are also checked against the live config, so pre-provisioning a type in a container it isn't registered with throws.
