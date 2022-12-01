using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogOtter.HttpPatch.Tests.Api;

public class TestApi : WebApplicationFactory<SystemTextStartup>
{
    private readonly SerializationEngine _serializationEngine;

    public TestApi(SerializationEngine serializationEngine)
    {
        _serializationEngine = serializationEngine;
    }

    public TestDataStore DataStore => Services.GetRequiredService<TestDataStore>();

    protected override IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSolutionRelativeContentRoot(Path.Combine("tests", GetType().Assembly.GetName().Name!));
        
        switch(_serializationEngine)
        {
            case SerializationEngine.Newtonsoft:
                builder.UseStartup<NewtonsoftStartup>();
                break;
            case SerializationEngine.SystemText:
                builder.UseStartup<SystemTextStartup>();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
                
        builder.ConfigureTestServices(sc =>
        {
            sc.AddSingleton<TestDataStore>();
        });

        builder.ConfigureLogging(options =>
        {
            options.AddFilter(logLevel => logLevel >= LogLevel.Warning);
            options.AddFilter("Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware",
                logLevel => logLevel >= LogLevel.Error);
        });
    }
}