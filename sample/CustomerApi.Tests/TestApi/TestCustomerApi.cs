using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using CustomerApi.Configuration;
using CustomerApi.Events.Customers;
using CustomerApi.NonEventSourcedData.CustomerInterests;
using CustomerApi.Services;
using LogOtter.CosmosDb;
using LogOtter.CosmosDb.EventStore;
using LogOtter.CosmosDb.Testing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CustomerApi.Tests;

public class TestCustomerApi : IDisposable
{
    private readonly TestApplicationFactory _hostedApi;

    private readonly bool _disableCosmosAutoProvisioning;

    public Uri BaseAddress => _hostedApi.Server.BaseAddress;

    public GivenSteps Given { get; }
    public ThenSteps Then { get; }

    public TestCustomerApi()
        : this(false) { }

    public TestCustomerApi(bool disableCosmosAutoProvisioning)
    {
        _disableCosmosAutoProvisioning = disableCosmosAutoProvisioning;
        _hostedApi = new TestApplicationFactory(ConfigureTestServices, ConfigureServices);

        Given = _hostedApi.Services.GetRequiredService<GivenSteps>();
        Then = _hostedApi.Services.GetRequiredService<ThenSteps>();
    }

    public void Dispose()
    {
        _hostedApi.Dispose();
    }

    private void ConfigureTestServices(IServiceCollection services)
    {
        if (_disableCosmosAutoProvisioning)
        {
            services.RemoveAll<AutoProvisionSettings>();
            services.AddSingleton(_ => new AutoProvisionSettings(false));
        }

        services
            .AddTestCosmosDb()
            .WithPreProvisionedContainer<SearchableInterest>("SearchableInterest")
            .WithPreProvisionedContainer<EmailAddressReservation>("EmailAddressReservations")
            .WithPreProvisionedContainer<CustomerInterest>("LookupItems")
            .WithPreProvisionedContainer<CustomerEvent>("CustomerEvents")
            .WithPreProvisionedContainer<CustomerReadModel>(
                "Customers",
                indexingPolicy: new IndexingPolicy().WithCompositeIndex(
                    new() { Path = "/LastName", Order = CompositePathSortOrder.Ascending },
                    new() { Path = "/FirstName", Order = CompositePathSortOrder.Ascending }
                )
            );

        services.AddTransient<ConsumerStore>();
        services.AddTransient<CustomerStore>();
        services.AddTransient<SearchableInterestStore>();
        services.AddTransient<GivenSteps>();
        services.AddTransient<ThenSteps>();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    SignatureValidator = (token, _) => new JwtSecurityToken(token),
                    ValidateAudience = false,
                    ValidateIssuer = false
                };
            }
        );

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

    private class TestApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly Action<IServiceCollection> _configureServices;
        private readonly Action<IServiceCollection> _configureTestServices;

        public TestApplicationFactory(Action<IServiceCollection> configureTestServices, Action<IServiceCollection> configureServices)
        {
            _configureTestServices = configureTestServices;
            _configureServices = configureServices;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .ConfigureTestServices(_configureTestServices)
                .ConfigureServices(_configureServices)
                .ConfigureLogging(options =>
                {
                    options.AddFilter(logLevel => logLevel >= LogLevel.Warning);
                    options.AddFilter("Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware", logLevel => logLevel >= LogLevel.Error);
                });
        }
    }
}
