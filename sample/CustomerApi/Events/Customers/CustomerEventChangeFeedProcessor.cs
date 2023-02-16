using LogOtter.CosmosDb;

namespace CustomerApi.Events.Customers;

public class CustomerEventChangeFeedProcessor : IChangeFeedProcessorChangeHandler<CustomerEvent>
{
    private readonly ILogger<CustomerEventChangeFeedProcessor> _logger;

    public CustomerEventChangeFeedProcessor(ILogger<CustomerEventChangeFeedProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessChanges(IReadOnlyCollection<CustomerEvent> changes, CancellationToken cancellationToken)
    {
        foreach (var customerEvent in changes)
        {
            _logger.LogInformation("Processing {EventType} for {CustomerUri}",
                customerEvent.GetType().Name,
                customerEvent.CustomerUri
            );
        }

        return Task.CompletedTask;
    }
}
