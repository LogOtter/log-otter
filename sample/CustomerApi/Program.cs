using System.Collections.ObjectModel;
using CustomerApi.Configuration;
using CustomerApi.Events.Customers;
using CustomerApi.HealthChecks;
using LogOtter.CosmosDb;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services
    .AddAuthentication()
    .AddJwtBearer();

services.Configure<CosmosDbOptions>(configuration.GetSection("CosmosDb"));
services.Configure<PageOptions>(configuration.GetSection("PageOptions"));

services
    .AddCosmosDb()
    .AddEventStore(options => options.AutoEscapeIds = true)
    .AddEventSource<CustomerEvent, CustomerReadModel>("CustomerEvent")
    .AddSnapshotStoreProjection<CustomerEvent, CustomerReadModel>("Customers",
        compositeIndexes: new[]
        {
            new Collection<CompositePath>
            {
                new() { Path = "/LastName", Order = CompositePathSortOrder.Ascending },
                new() { Path = "/FirstName", Order = CompositePathSortOrder.Ascending }
            }
        }
    )
    .AddCatchupSubscription<CustomerEvent,
        TestCustomerEventCatchupSubscription>("TestCustomerEventCatchupSubscription");

services
    .AddHealthChecks()
    .AddCheck<ResolveAllControllersHealthCheck>("Resolve All Controllers");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseHealthChecks("/healthcheck");

app.MapControllers();

app.Run();