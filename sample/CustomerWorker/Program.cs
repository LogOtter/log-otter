using CustomerWorker.HealthChecks;
using CustomerWorker.Services;
using LogOtter.SimpleHealthChecks;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", false).AddEnvironmentVariables();
    })
    .ConfigureServices(
        (_, services) =>
        {
            services.AddApplicationInsightsTelemetryWorkerService();

            services
                .AddHealthChecks()
                .AddCheck<AlwaysHealthyCheck>("StartupCheck", tags: new[] { "ready" })
                .AddCheck<AlwaysHealthyCheck>("CustomCheck");

            services
                .AddSimpleHealthChecks()
                .AddEndpoint("/health")
                .AddEndpoint("/health/detailed", new SimpleHealthCheckOptions { ResponseWriter = ResponseWriter.WriteDetailedJson })
                .AddEndpoint("/health/ready", new SimpleHealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
                .AddEndpoint("/health/live");

            services.AddHostedService<DummyWorker>();
        }
    )
    .Build();

await host.RunAsync();
