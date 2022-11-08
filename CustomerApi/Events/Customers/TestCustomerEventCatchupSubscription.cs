using LogOtter.EventStore.CosmosDb;

namespace CustomerApi.Events.Customers;

public class TestCustomerEventCatchupSubscription : ICatchupSubscription<CustomerEvent>
{
    private readonly ILogger<TestCustomerEventCatchupSubscription> _logger;

    public TestCustomerEventCatchupSubscription(ILogger<TestCustomerEventCatchupSubscription> logger)
    {
        _logger = logger;
    }

    public Task ProcessEvents(IReadOnlyCollection<Event<CustomerEvent>> events, CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            _logger.LogInformation(
                "TestCustomerEventCatchupSubscription: {EventName} : {CustomerUri}",
                @event.Body.GetType().Name,
                @event.Body.CustomerUri
            );
        }

        return Task.CompletedTask;
    }
}