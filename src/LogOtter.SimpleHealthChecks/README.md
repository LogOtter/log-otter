# SimpleHealthChecks

A library to expose health checks over HTTP for use in worker services without using the `Microsoft.NET.Sdk.Web` SDK.

## Usage

1. Register `Microsoft.Extensions.Diagnostics.HealthChecks` as normal

```c#
services
    .AddHealthChecks()
    .AddCheck<MyCustomCheck>("CustomCheck")
```

2. Register the SimpleHealthChecks and expose an endpoint

```c#
services
    .AddSimpleHealthChecks()
    .AddEndpoint("/health")
```

## Options

By default, the health checks we be exposed on port 80 on all IP addresses. To configure, 
set the `SimpleHealthCheckHostOptions`

```c#
services
    .AddSimpleHealthChecks(options => {
        options.Host = "127.0.0.1";
        options.Port = 8080;
    });
```

## Customize

Healthcheck endpoints can be filter checks and customize their response in the same way as the 
`Microsoft.Extensions.Diagnostics.HealthChecks` can.

### Example: Filter checks for different endpoints

```c#
services
    .AddSimpleHealthChecks()
    .AddEndpoint("/health/ready", new SimpleHealthCheckOptions()
    {
        Predicate = check => check.Tags.Contains("ready"),
    })
    .AddEndpoints("/health/live", new SimpleHealthCheckOptions());
```

### Example: Custom response

```c#
services
    .AddSimpleHealthChecks()
    .AddEndpoint("/health", new SimpleHealthCheckOptions
    {
        ResponseWriter = WriteResponse
    });
```

```c#
private static Task WriteResponse(HttpListenerContext context, HealthReport healthReport)
{
    context.Response.Headers[HttpRequestHeader.ContentType] = "application/json; charset=utf-8";

    var options = new JsonWriterOptions { Indented = true };

    using var memoryStream = new MemoryStream();
    using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("status", healthReport.Status.ToString());
        jsonWriter.WriteStartObject("results");

        foreach (var healthReportEntry in healthReport.Entries)
        {
            jsonWriter.WriteStartObject(healthReportEntry.Key);
            jsonWriter.WriteString("status", healthReportEntry.Value.Status.ToString());
            jsonWriter.WriteString("description", healthReportEntry.Value.Description);
            jsonWriter.WriteStartObject("data");

            foreach (var item in healthReportEntry.Value.Data)
            {
                jsonWriter.WritePropertyName(item.Key);

                JsonSerializer.Serialize(jsonWriter, item.Value, item.Value?.GetType() ?? typeof(object));
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndObject();
    }

    return context.Response.OutputStream.WriteAsync(memoryStream.ToArray().AsMemory()).AsTask();
}
```

## Running locally

In order to run locally, you'll either need to run with Admin permissions or run `netsh` as an administrator first:

```pwsh
# Run as Administrator
# Note: change port 80 if required
netsh http add urlacl url=http://+:80/ user=$env:USERDOMAIN\$env:USERNAME
```
