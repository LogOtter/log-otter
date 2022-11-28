using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using CustomerApi.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CustomerApi.Tests;

public class TestCustomerApi : IDisposable
{
    private readonly TestApplicationFactory _hostedApi;

    public Uri BaseAddress => _hostedApi.Server.BaseAddress;

    public GivenSteps Given { get; }
    public ThenSteps Then { get; }

    public TestCustomerApi()
    {
        _hostedApi = new TestApplicationFactory(ConfigureTestServices, ConfigureServices);

        Given = _hostedApi.Services.GetRequiredService<GivenSteps>();
        Then = _hostedApi.Services.GetRequiredService<ThenSteps>();
    }

    private void ConfigureTestServices(IServiceCollection services)
    {
        services.AddTestCosmosDb();

        services.AddTransient<ConsumerStore>();
        services.AddTransient<CustomerStore>();
        services.AddTransient<GivenSteps>();
        services.AddTransient<ThenSteps>();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                SignatureValidator = (token, _) => new JwtSecurityToken(token),
                ValidateAudience = false,
                ValidateIssuer = false
            };
        });

        services.PostConfigure<PageOptions>(options =>
        {
            options.PageSize = 5;
        });
    }

    public HttpClient CreateClient(AuthenticationHeaderValue? authenticationHeaderValue = null)
    {
        var client = _hostedApi.CreateClient();
        client.DefaultRequestHeaders.Authorization = authenticationHeaderValue;
        return client;
    }

    public void Dispose()
    {
        _hostedApi.Dispose();
    }

    private class TestApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly Action<IServiceCollection> _configureTestServices;
        private readonly Action<IServiceCollection> _configureServices;

        public TestApplicationFactory(
            Action<IServiceCollection> configureTestServices,
            Action<IServiceCollection> configureServices
        )
        {
            _configureTestServices = configureTestServices;
            _configureServices = configureServices;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .ConfigureTestServices(_configureTestServices)
                .ConfigureServices(_configureServices);
        }
    }
}