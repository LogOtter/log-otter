using System.Collections.ObjectModel;
using CustomerApi.Configuration;
using CustomerApi.Events.Customers;
using CustomerApi.Events.Movies;
using CustomerApi.HealthChecks;
using CustomerApi.NonEventSourcedData.CustomerInterests;
using CustomerApi.Services;
using LogOtter.Azure.AppServices.RequestMiddleware;
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
services.AddApplicationInsightsTelemetry();

services.AddAuthentication().AddJwtBearer();

services.Configure<CosmosDbOptions>(configuration.GetSection("CosmosDb"));
services.Configure<PageOptions>(configuration.GetSection("PageOptions"));
services.Configure<EventStreamsApiOptions>(configuration.GetSection("EventStreamsApi"));
services.AddSingleton<EmailAddressReservationService>();

services
    .AddCosmosDb()
    .WithAutoProvisioning()
    .AddContainer<SearchableInterest>("SearchableInterest")
    .AddContainer<EmailAddressReservation>(
        "EmailAddressReservations",
        c =>
        {
            c.WithChangeFeedProcessor<TestEmailAddressReservationChangeFeedProcessor>("TestEmailAddressReservationChangeFeedProcessor");
        }
    )
    .AddContainer<CustomerInterest>(
        "LookupItems",
        c =>
        {
            c.WithSubTypeThatMustHaveDistinctPartition<Movie>()
                .WithSubTypeThatMustHaveDistinctPartition<Song>()
                .WithChangeFeedProcessor<SearchableInterestsProcessor>("SearchableInterestChangeFeedProcessor");
        }
    )
    .AddEventSourcing(options => options.AutoEscapeIds = true)
    .AddEventSource<CustomerEvent>(
        "CustomerEvents",
        c =>
        {
            c.AddProjection<CustomerReadModel>()
                .WithSnapshot("Customers", _ => CustomerReadModel.StaticPartitionKey)
                .WithAutoProvisionSettings(
                    indexingPolicy: new IndexingPolicy().WithCompositeIndex(
                        new() { Path = "/LastName", Order = CompositePathSortOrder.Ascending },
                        new() { Path = "/FirstName", Order = CompositePathSortOrder.Ascending }
                    )
                );

            c.AddCatchupSubscription<TestCustomerEventCatchupSubscription>("TestCustomerEventCatchupSubscription");
        }
    )
    .AddEventSource<MovieEvent>(
        "MovieEvents",
        c =>
        {
            c.AddProjection<MovieReadModel>().WithSnapshot("Movies", _ => MovieReadModel.StaticPartitionKey);

            c.SpecifyEnabledFunc(_ => Task.FromResult(false));
        }
    );

services.AddHealthChecks().AddCheck<ResolveAllControllersHealthCheck>("Resolve All Controllers");

if (environment.IsDevelopment())
{
    services.AddCors(options => options.AddDefaultPolicy(c => c.WithOrigins("http://localhost:5173")));
}

var app = builder.Build();

app.UseRestoreRawRequestPathMiddleware();
app.UseCors();
app.UseAuthorization();

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
