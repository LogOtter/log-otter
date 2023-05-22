using CustomerApi.Services;
using LogOtter.CosmosDb;

namespace CustomerApi.Events.Customers;

public class TestEmailAddressReservationChangeFeedProcessor : IChangeFeedProcessorChangeHandler<EmailAddressReservation>
{
    private readonly ILogger<TestEmailAddressReservationChangeFeedProcessor> _logger;

    public TestEmailAddressReservationChangeFeedProcessor(ILogger<TestEmailAddressReservationChangeFeedProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessChanges(IReadOnlyCollection<EmailAddressReservation> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            _logger.LogInformation("TestEmailAddressReservationChangeFeedProcessor: {EmailAddress}", change.EmailAddress);
        }

        return Task.CompletedTask;
    }
}
