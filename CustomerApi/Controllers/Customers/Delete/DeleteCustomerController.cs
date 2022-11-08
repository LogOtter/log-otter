using CustomerApi.Events.Customers;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Customers.Delete;

[ApiController]
[Route("customers")]
public class DeleteCustomerController : ControllerBase
{
    private readonly EventRepository<CustomerEvent, CustomerReadModel> _customerEventRepository;

    public DeleteCustomerController(EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository)
    {
        _customerEventRepository = customerEventRepository;
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string id)
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

        var customerDeleted = new CustomerDeleted(
            customerUri,
            DateTime.UtcNow
        );

        await _customerEventRepository.ApplyEvents(customerUri.Uri, customerReadModel.Revision, customerDeleted);

        return NoContent();
    }
}