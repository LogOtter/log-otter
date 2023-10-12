using LogOtter.Azure.AppServices.RequestMiddleware;
using LogOtter.Hub.Configuration;
using LogOtter.Hub.Services;

var builder = WebApplication.CreateBuilder(args);

var serviceOptions = builder.GetServiceOptions();

var services = builder.Services;

services.AddSingleton(serviceOptions);
services.AddSingleton<EventStreamCache>();
services.AddHttpClient();
services.AddControllers();
services.AddReverseProxy().Initialize();

if (builder.Environment.IsDevelopment())
{
    services.AddCors(options => options.AddDefaultPolicy(c => c.WithOrigins("http://localhost:5173")));
}

var app = builder.Build();

app.ConfigureReverseProxy(serviceOptions);

app.UseRestoreRawRequestPathMiddleware();
app.UseCors();
app.UseFileServer();
app.UseRouting();
app.MapControllers();
app.MapReverseProxy();

app.Run();
