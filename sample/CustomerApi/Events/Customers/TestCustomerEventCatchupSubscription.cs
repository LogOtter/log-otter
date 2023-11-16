using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Customers;

public class TestCustomerEventCatchupSubscription(ILogger<TestCustomerEventCatchupSubscription> logger) : ICatchupSubscription<CustomerEvent>
{
    public Task ProcessEvents(IReadOnlyCollection<Event<CustomerEvent>> events, CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            logger.LogInformation(
                "TestCustomerEventCatchupSubscription: {EventName} : {CustomerUri}",
                @event.Body.GetType().Name,
                @event.Body.CustomerUri
            );
        }

        return Task.CompletedTask;
    }
}
