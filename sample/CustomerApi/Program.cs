using System.Collections.ObjectModel;
using CustomerApi.Configuration;
using CustomerApi.Events.Customers;
using CustomerApi.HealthChecks;
using LogOtter.CosmosDb;
using LogOtter.CosmosDb.EventStore;
using LogOtter.CosmosDb.EventStore.EventStreamApi;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment;
var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddAuthentication().AddJwtBearer();

services.Configure<CosmosDbOptions>(configuration.GetSection("CosmosDb"));
services.Configure<PageOptions>(configuration.GetSection("PageOptions"));
services.Configure<EventStreamsApiOptions>(configuration.GetSection("EventStreamsApi"));

services.AddCosmosDb()
        .AddEventStore(options => options.AutoEscapeIds = true)
        .AddEventSource<CustomerEvent, CustomerReadModel>("CustomerEvent", _ => CustomerReadModel.StaticPartitionKey)
        .AddSnapshotStoreProjection<CustomerEvent, CustomerReadModel>(
            "Customers",
            compositeIndexes: new[]
            {
                new Collection<CompositePath>
                {
                    new() { Path = "/LastName", Order = CompositePathSortOrder.Ascending },
                    new() { Path = "/FirstName", Order = CompositePathSortOrder.Ascending }
                }
            })
        .AddCatchupSubscription<CustomerEvent, TestCustomerEventCatchupSubscription>("TestCustomerEventCatchupSubscription");

services.AddHealthChecks().AddCheck<ResolveAllControllersHealthCheck>("Resolve All Controllers");

if (environment.IsDevelopment())
{
    services.AddCors(options => options.AddDefaultPolicy(c => c.WithOrigins("http://localhost:5173")));
}

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseEventStreamsApi();
    app.UseEventStreamsUI();
}

app.UseHttpsRedirection();

app.UseHealthChecks("/health");

app.MapControllers();

app.Run();
