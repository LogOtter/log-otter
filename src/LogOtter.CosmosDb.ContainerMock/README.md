# CosmosDB ContainerMock

An in-memory version of a Cosmos Container for use with testing.

Like Cosmos DB, the mock rejects documents that don't have an `id` property, or whose value at the container's partition key path doesn't match the partition key passed in. The error explains how to fix the document, for example when a property is serialized as `Id` instead of `id`.

> ⚠️ Note: Not every method is implemented. Please raise an issue or pull request for anything that is missing that you require.
