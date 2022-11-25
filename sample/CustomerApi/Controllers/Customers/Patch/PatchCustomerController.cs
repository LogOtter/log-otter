using CustomerApi.Events.Customers;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using LogOtter.HttpPatch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Customers.Patch;

[ApiController]
[Route("customers")]
[Authorize(Roles = "Customers.ReadWrite")]
public class PatchCustomerController : ControllerBase
{
    private readonly EventRepository<CustomerEvent, CustomerReadModel> _customerEventRepository;

    public PatchCustomerController(EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository)
    {
        _customerEventRepository = customerEventRepository;
    }

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

        var customerReadModel = await _customerEventRepository.Get(
            customerUri.Uri,
            includeDeleted: true,
            cancellationToken: cancellationToken
        );

        if (customerReadModel == null)
        {
            return NotFound();
        }

        var events = new List<CustomerEvent>();

        if (request.EmailAddress.IsIncludedInPatch)
        {
            events.Add(new CustomerEmailAddressChanged(
                customerUri,
                customerReadModel.EmailAddress,
                request.EmailAddress.Value!,
                DateTimeOffset.UtcNow
            ));
        }

        if (request.FirstName.IsIncludedInPatch || request.LastName.IsIncludedInPatch)
        {
            var newFirstName = request.FirstName.GetValueIfIncludedOrDefault(customerReadModel.FirstName);
            var newLastName = request.LastName.GetValueIfIncludedOrDefault(customerReadModel.LastName);

            events.Add(new CustomerNameChanged(
                customerUri,
                customerReadModel.FirstName,
                newFirstName,
                customerReadModel.LastName,
                newLastName,
                DateTimeOffset.UtcNow
            ));
        }

        var updatedCustomer = await _customerEventRepository.ApplyEvents(
            customerUri.Uri,
            customerReadModel.Revision,
            cancellationToken,
            events.ToArray()
        );

        return Ok(new CustomerResponse(updatedCustomer));
    }
}

public record PatchCustomerRequest(
    [RequiredIfPatched, EmailAddressIfPatched]
    OptionallyPatched<string> EmailAddress,
    [RequiredIfPatched] OptionallyPatched<string> FirstName,
    [RequiredIfPatched] OptionallyPatched<string> LastName
) : BasePatchRequest;