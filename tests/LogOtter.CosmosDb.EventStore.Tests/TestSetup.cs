using LogOtter.CosmosDb.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class TestSetup : IDisposable
{
    private IHost _app;
    public EventualEventRepository<MessageEvent, Message> EventualEventRepository { get; }
    public EventualSnapshotRepository<MessageEvent, Message> EventualSnapshotRepository { get; }

    public TestSetup()
    {
        var host = Host.CreateDefaultBuilder();
        host.ConfigureServices(services =>
        {
            services
                .AddCosmosDb()
                .WithAutoProvisioning()
                .AddEventSourcing(c => c.AutoEscapeIds = true)
                .AddEventualEvent<MessageEvent>("MessageEvents", c => c.AddProjection<Message>().WithSnapshot("Messages", e => e.EventStreamId));

            services.AddTestCosmosDb();
        });

        _app = host.Build();
        _app.Start();

        EventualEventRepository = _app.Services.GetRequiredService<EventualEventRepository<MessageEvent, Message>>();
        EventualSnapshotRepository = _app.Services.GetRequiredService<EventualSnapshotRepository<MessageEvent, Message>>();
    }

    public async void Dispose()
    {
        await _app.StopAsync();
    }
}
