using CustomerApi.Services;
using LogOtter.CosmosDb;

namespace CustomerApi.Events.Customers;

public class TestEmailAddressReservationChangeFeedProcessor(ILogger<TestEmailAddressReservationChangeFeedProcessor> logger)
    : IChangeFeedProcessorChangeHandler<EmailAddressReservation>
{
    public Task ProcessChanges(IReadOnlyCollection<EmailAddressReservation> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            logger.LogInformation("TestEmailAddressReservationChangeFeedProcessor: {EmailAddress}", change.EmailAddress);
        }

        return Task.CompletedTask;
    }
}
