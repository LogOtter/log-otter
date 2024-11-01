using LogOtter.CosmosDb.EventStore;

namespace CustomerApi.Events.Customers;

public class TestCustomerEventCatchupSubscription(
    ILogger<TestCustomerEventCatchupSubscription> logger,
    EventRepository<CustomerEvent, CustomerReadModel> eventRepository
) : ICatchupSubscription<CustomerEvent>
{
    public async Task ProcessEvents(IReadOnlyCollection<Event<CustomerEvent>> events, CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            await (
                @event.Body switch
                {
                    CustomerEmailAddressChanged customerCreated => HandleEmailAddresses(customerCreated),
                    _ => Task.CompletedTask
                }
            );
            logger.LogInformation(
                "TestCustomerEventCatchupSubscription: {EventName} : {CustomerUri}",
                @event.Body.GetType().Name,
                @event.Body.CustomerUri
            );
        }

        return;
    }

    private async Task HandleEmailAddresses(CustomerEmailAddressChanged ev)
    {
        var existing = await eventRepository.Get(ev.CustomerUri.Uri, null);
        if (existing == null)
        {
            return;
        }
        await eventRepository.ApplyEvents(
            ev.CustomerUri.Uri,
            existing.Revision,
            new CustomerEmailAddressesChanged(ev.CustomerUri, [ev.OldEmailAddress], [ev.NewEmailAddress], ev.Timestamp)
        );
    }
}
