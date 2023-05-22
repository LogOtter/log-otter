using System.Net;
using LogOtter.CosmosDb;
using Microsoft.Azure.Cosmos;

namespace CustomerApi.Services;

public class EmailAddressReservationService
{
    private readonly CosmosContainer<EmailAddressReservation> _emailAddressReservationContainer;

    public EmailAddressReservationService(CosmosContainer<EmailAddressReservation> emailAddressReservationContainer)
    {
        _emailAddressReservationContainer = emailAddressReservationContainer;
    }

    public async Task ReserveEmailAddress(string emailAddress)
    {
        var reservation = new EmailAddressReservation(emailAddress);

        try
        {
            await _emailAddressReservationContainer.Container.CreateItemAsync(reservation, new PartitionKey(reservation.PartitionKey));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            throw new EmailAddressInUseException(emailAddress);
        }
    }

    public async Task ReleaseEmailAddress(string emailAddress)
    {
        var reservation = new EmailAddressReservation(emailAddress);

        await _emailAddressReservationContainer.Container.DeleteItemAsync<EmailAddressReservation>(
            reservation.Id,
            new PartitionKey(reservation.PartitionKey)
        );
    }
}
