using CustomerApi.Events.Customers;
using CustomerApi.Services;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using LogOtter.HttpPatch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Customers.Patch;

[ApiController]
[Route("customers")]
[Authorize(Roles = "Customers.ReadWrite")]
public class PatchCustomerController(
    EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository,
    EmailAddressReservationService emailAddressReservationService
) : ControllerBase
{
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> Patch(
        [FromRoute] string id,
        [FromBody] PatchCustomerRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Id.TryParse(id, out var customerId))
        {
            return NotFound();
        }

        var customerUri = new CustomerUri(customerId);

        var customerReadModel = await customerEventRepository.Get(customerUri.Uri, cancellationToken: cancellationToken);

        if (customerReadModel == null)
        {
            return NotFound();
        }

        var events = new List<CustomerEvent>();

        if (request.EmailAddress.IsIncludedInPatchAndDifferentFrom(customerReadModel.EmailAddress))
        {
            events.Add(
                new CustomerEmailAddressChanged(customerUri, customerReadModel.EmailAddress, request.EmailAddress.Value!, DateTimeOffset.UtcNow)
            );

            await emailAddressReservationService.ReleaseEmailAddress(customerReadModel.EmailAddress);
            await emailAddressReservationService.ReserveEmailAddress(request.EmailAddress.Value!);
        }

        if (
            request.FirstName.IsIncludedInPatchAndDifferentFrom(customerReadModel.FirstName)
            || request.LastName.IsIncludedInPatchAndDifferentFrom(customerReadModel.LastName)
        )
        {
            var newFirstName = request.FirstName.GetValueIfIncludedOrDefault(customerReadModel.FirstName);
            var newLastName = request.LastName.GetValueIfIncludedOrDefault(customerReadModel.LastName);

            events.Add(
                new CustomerNameChanged(
                    customerUri,
                    customerReadModel.FirstName,
                    newFirstName,
                    customerReadModel.LastName,
                    newLastName,
                    DateTimeOffset.UtcNow
                )
            );
        }

        if (events.Count == 0)
        {
            return Ok(new CustomerResponse(customerReadModel));
        }

        var updatedCustomer = await customerEventRepository.ApplyEvents(
            customerUri.Uri,
            customerReadModel.Revision,
            cancellationToken,
            events.ToArray()
        );

        return Ok(new CustomerResponse(updatedCustomer));
    }
}

public record PatchCustomerRequest(
    [RequiredIfPatched] [EmailAddressIfPatched] OptionallyPatched<string> EmailAddress,
    [RequiredIfPatched] OptionallyPatched<string> FirstName,
    [RequiredIfPatched] OptionallyPatched<string> LastName
) : BasePatchRequest;
