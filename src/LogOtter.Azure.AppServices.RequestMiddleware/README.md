> ⚠️ Warning: LogOtter is still in beta and there are likely to be breaking changes prior to a v1 release. Use at your own peril!

# Azure AppServices RequestMiddleware

Adds middleware for converting the request back to the original request (Azure AppServices
decode path strings and there is currently no way to disable).

For more information see [dotnet/aspnetcore#40532](https://github.com/dotnet/aspnetcore/issues/40532)
and [Azure/azure-functions-host#9402](https://github.com/Azure/azure-functions-host/pull/9402#issuecomment-1747347531)

## Examples

Register the middleware

```c#
app.UseRestoreRawRequestPathMiddleware();
```
