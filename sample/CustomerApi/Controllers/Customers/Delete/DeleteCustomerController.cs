using CustomerApi.Events.Customers;
using CustomerApi.Services;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Customers.Delete;

[ApiController]
[Route("customers")]
[Authorize(Roles = "Customers.Delete")]
public class DeleteCustomerController : ControllerBase
{
    private readonly EventRepository<CustomerEvent, CustomerReadModel> _customerEventRepository;
    private readonly EmailAddressReservationService _emailAddressReservationService;

    public DeleteCustomerController(
        EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository,
        EmailAddressReservationService emailAddressReservationService
    )
    {
        _customerEventRepository = customerEventRepository;
        _emailAddressReservationService = emailAddressReservationService;
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken cancellationToken)
    {
        if (!Id.TryParse(id, out var customerId))
        {
            return NotFound();
        }

        var customerUri = new CustomerUri(customerId);

        var customerReadModel = await _customerEventRepository.Get(customerUri.Uri, includeDeleted: true);

        if (customerReadModel == null)
        {
            return NotFound();
        }

        if (customerReadModel.DeletedAt.HasValue)
        {
            return NoContent();
        }

        await _emailAddressReservationService.ReleaseEmailAddress(customerReadModel.EmailAddress);

        var customerDeleted = new CustomerDeleted(customerUri);

        await _customerEventRepository.ApplyEvents(customerUri.Uri, customerReadModel.Revision, cancellationToken, customerDeleted);

        return NoContent();
    }
}
