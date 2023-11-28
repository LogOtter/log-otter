# Log Otter Hub

Log Otter Hub aggregates the Event Stream viewer from multiple services in to a single UI.

> ⚠️ Note: currently the Hub does not support any authentication, see below on guide on how to configure. 
> Future versions are planned to support authentication.

## Configure

1. Use the latest LogOtter hub docker image ([`ghcr.io/logotter/hub:latest`](https://github.com/LogOtter/log-otter/pkgs/container/hub))
2. Configure the services:
   * Mount a `services.json` in the root _or_
   * Specify the contents of `services.json` as environment variables

### Configure via Environment Variables

For each service, add two environment variables as shown below, 
incrementing the index by one each time:

```
Hub__Services__<Index>__Name = <ServiceName>
Hub__Services__<Index>__Url  = <ServiceUrl>
```

#### Example

```
Hub__Services__0__Name = CustomerApi
Hub__Services__0__Url  = https://customer-api/logotter/api
Hub__Services__1__Name = MoviesApi
Hub__Services__1__Url  = https://movies-api/logotter/api
```

## Suggested Setup

Each service will need to expose the Event Streams API, e.g.

```csharp
app.UseEventStreamsApi();
```

⚠️ Due to authentication not being currently supported, these should be not be 
exposed publicly and ingress/API gateway should only allow access to the 
internal network.

Host the hub on the same vnet/within the same k8s cluster so it can access
these APIs.
