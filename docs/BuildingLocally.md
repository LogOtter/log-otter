# Building Locally

## Dependencies

* [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
* [Docker](https://www.docker.com/products/docker-desktop/)

## Authentication

To get an access token for calling the sample API, use `dotnet user-jwts`. For example:

```
cd sample\CustomerApi
dotnet user-jwts create --name "Bob Bobertson" --role "Customers.Read" --role "Customers.ReadWrite" --role "Customers.Delete" --role "Customers.Create"
```

## Integration Tests

The `LogOtter.CosmosDb.ContainerMock.IntegrationTests` project uses Testcontainers to run
a CosmosDB Emulator in a docker container. On first run, this can sometimes fail when run
in VS/Rider with the error:

```
Status 404: {"message":"No such image: testcontainers/ryuk:0.3.4"}
```

To fix this issue, ensure you have pulled the `testcontainers/ryuk` container.

```
docker pull testcontainers/ryuk:0.3.4
```