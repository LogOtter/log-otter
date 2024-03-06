using System.Net;
using LogOtter.CosmosDb;
using Microsoft.Azure.Cosmos;

namespace CustomerApi.Services;

public class EmailAddressReservationService(CosmosContainer<EmailAddressReservation> emailAddressReservationContainer)
{
    public async Task ReserveEmailAddress(string emailAddress)
    {
        var reservation = new EmailAddressReservation(emailAddress);

        try
        {
            await emailAddressReservationContainer.Container.CreateItemAsync(reservation, new PartitionKey(reservation.PartitionKey));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            throw new EmailAddressInUseException(emailAddress);
        }
    }

    public async Task ReleaseEmailAddress(string emailAddress)
    {
        var reservation = new EmailAddressReservation(emailAddress);

        await emailAddressReservationContainer.Container.DeleteItemAsync<EmailAddressReservation>(
            reservation.Id,
            new PartitionKey(reservation.PartitionKey)
        );
    }
}
