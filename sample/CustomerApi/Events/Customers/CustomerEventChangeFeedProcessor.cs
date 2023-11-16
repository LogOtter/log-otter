using LogOtter.CosmosDb;

namespace CustomerApi.Events.Customers;

public class CustomerEventChangeFeedProcessor(ILogger<CustomerEventChangeFeedProcessor> logger) : IChangeFeedProcessorChangeHandler<CustomerEvent>
{
    public Task ProcessChanges(IReadOnlyCollection<CustomerEvent> changes, CancellationToken cancellationToken)
    {
        foreach (var customerEvent in changes)
        {
            logger.LogInformation("Processing {EventType} for {CustomerUri}", customerEvent.GetType().Name, customerEvent.CustomerUri);
        }

        return Task.CompletedTask;
    }
}
