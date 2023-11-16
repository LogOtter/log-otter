using CustomerApi.Events.Customers;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Customers.GetById;

[ApiController]
[Route("customers")]
[Authorize(Roles = "Customers.Read,Customers.ReadWrite")]
public class GetCustomerByIdController(EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository) : ControllerBase
{
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetById([FromRoute] string id, CancellationToken cancellationToken)
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

        return Ok(new CustomerResponse(customerReadModel));
    }
}
