using CustomerApi.Events.Customers;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Customers.Compact;

[ApiController]
[Route("customers")]
[Authorize(Roles = "Customers.Compact")]
public class CompactCustomerController(
    EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository,
    StreamCompactionService<CustomerEvent, CustomerReadModel> compactionService
) : ControllerBase
{
    [HttpPost("{id}/compact")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Compact([FromRoute] string id, CancellationToken cancellationToken)
    {
        if (!Id.TryParse(id, out var customerId))
        {
            return NotFound();
        }

        var customerUri = new CustomerUri(customerId);

        var customerReadModel = await customerEventRepository.Get(customerUri.Uri, includeDeleted: true, cancellationToken: cancellationToken);

        if (customerReadModel == null)
        {
            return NotFound();
        }

        await compactionService.CompactStream(customerUri.Uri, cancellationToken);

        return NoContent();
    }
}
