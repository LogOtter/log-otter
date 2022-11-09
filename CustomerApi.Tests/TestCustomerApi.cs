using LogOtter.CosmosDb.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerApi.Tests;

public class TestCustomerApi : IDisposable
{
    private readonly TestApplicationFactory _hostedApi;
    
    public GivenSteps Given { get; }

    public TestCustomerApi()
    {
        _hostedApi = new TestApplicationFactory(ConfigureTestServices);

        Given = _hostedApi.Services.GetRequiredService<GivenSteps>();
    }

    private void ConfigureTestServices(IServiceCollection services)
    {
        services.AddTestCosmosDb();

        services.AddTransient<CustomerStore>();
        services.AddTransient<GivenSteps>();
    }

    public HttpClient CreateClient()
    {
        return _hostedApi.CreateClient();
    }

    public void Dispose()
    {
        _hostedApi.Dispose();
    }

    private class TestApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly Action<IServiceCollection> _configureTestServices;

        public TestApplicationFactory(Action<IServiceCollection> configureTestServices)
        {
            _configureTestServices = configureTestServices;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(_configureTestServices);
        }
    }
}